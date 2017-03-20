using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SwitchWithoutDefaultCodeFixProvider)), Shared]
    public class SwitchWithoutDefaultCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.SwitchCaseWithoutDefault.ToDiagnosticId());
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Add a default clause", c => SwitchWithoutDefaultAsync(context.Document, diagnostic, c),
                                    nameof(SwitchWithoutDefaultCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> SwitchWithoutDefaultAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newSections = new List<SwitchSectionSyntax>();
            var switchCaseStatement = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<SwitchStatementSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var expressionSymbol = semanticModel.GetSymbolInfo(switchCaseStatement.Expression).Symbol;
            if (semanticModel.GetTypeInfo(switchCaseStatement.Expression).Type.SpecialType == SpecialType.System_Boolean)
            {
                var type = ((CaseSwitchLabelSyntax)switchCaseStatement.Sections.Last().ChildNodes().First()).Value.GetFirstToken().Text;
                var againstType = type == "true" ? "false" : "true";
                newSections.Add(SyntaxFactory.SwitchSection().WithLabels(
                    new SyntaxList<SwitchLabelSyntax>().Add(SyntaxFactory.CaseSwitchLabel(SyntaxFactory.ParseExpression(againstType))))
                    .WithStatements(new SyntaxList<StatementSyntax>().Add(SyntaxFactory.ParseStatement("break;"))));
            }
            else
            {
                newSections.Add(CreateSection(SyntaxFactory.DefaultSwitchLabel(),
                    SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(SyntaxFactory.ParseTypeName("System.Exception").WithAdditionalAnnotations(Simplifier.Annotation),
                    SyntaxFactory.ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>().Add(SyntaxFactory.Argument(SyntaxFactory.ParseExpression("\"Unexpected Case\"")))), null))));
            }
            var newsSwitchCaseStatement = switchCaseStatement
                            .AddSections(newSections.ToArray())
                            .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(switchCaseStatement, newsSwitchCaseStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        static SwitchSectionSyntax CreateSection(SwitchLabelSyntax label, StatementSyntax statement)
        {
            var labels = new SyntaxList<SwitchLabelSyntax>();
            labels = labels.Add(label);
            return SyntaxFactory.SwitchSection(labels, CreateSectionStatements(statement));
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
            return result;
        }
    }
}
