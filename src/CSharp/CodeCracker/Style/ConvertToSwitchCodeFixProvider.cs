using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{

    [ExportCodeFixProvider(nameof(ConvertToSwitchCodeFixProvider), LanguageNames.CSharp), Shared]
    public class ConvertToSwitchCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ConvertToSwitch.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var ifStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
            const string message = "Convert to 'switch'";
            context.RegisterCodeFix(CodeAction.Create(message, c => ConvertToSwitchAsync(context.Document, ifStatement, c)), diagnostic);
        }

        private async Task<Document> ConvertToSwitchAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var nestedIfs = ConvertToSwitchAnalyzer.FindNestedIfs(ifStatement).ToArray();

            var sections = new List<SwitchSectionSyntax>();
            foreach (var nestedIf in nestedIfs)
            {
                var label = SyntaxFactory.CaseSwitchLabel(
                    ((BinaryExpressionSyntax)nestedIf.Condition).Right);

                if (nestedIf != ifStatement)
                {
                    label = label.WithLeadingTrivia(nestedIf.Parent.GetLeadingTrivia());
                }

                sections.Add(CreateSection(label, nestedIf.Statement));
            }

            var @else = nestedIfs.Last().Else;
            if (@else != null)
            {
                sections.Add(CreateSection(
                    SyntaxFactory.DefaultSwitchLabel()
                    .WithLeadingTrivia(@else.GetLeadingTrivia()),
                    @else.Statement
                    ));
            }

            var condition = ifStatement.Condition as BinaryExpressionSyntax;
            var switchExpression = SyntaxFactory.IdentifierName((condition.Left as IdentifierNameSyntax).Identifier.Text);

            var switchStatement = SyntaxFactory.SwitchStatement(switchExpression)
                .WithSections(new SyntaxList<SwitchSectionSyntax>().AddRange(sections))
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(ifStatement, switchStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        static SwitchSectionSyntax CreateSection(SwitchLabelSyntax label, StatementSyntax statement)
        {
            var labels = new SyntaxList<SwitchLabelSyntax>();
            labels = labels.Add(label);

            return SyntaxFactory.SwitchSection(
                labels, CreateSectionStatements(statement)
                );
        }

        static SyntaxList<StatementSyntax> CreateSectionStatements(StatementSyntax source)
        {
            var result = new SyntaxList<StatementSyntax>();

            if (source is BlockSyntax)
            {
                var block = source as BlockSyntax;
                result = result.AddRange(block.Statements);
            }
            else
            {
                result = result.Add(source);
            }

            var lastStatement = result.LastOrDefault();
            if (!(lastStatement is ReturnStatementSyntax || lastStatement is ThrowStatementSyntax))
            {
                result = result.Add(SyntaxFactory.BreakStatement());
            }

            return result;
        }
    }
}
