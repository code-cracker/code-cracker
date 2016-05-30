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
namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SwitchWithoutDefaultCodeFixProvider)), Shared]
    public class SwitchWithoutDefaultCodeFixProvider : CodeFixProvider
    {
        private const string EmptyString = "\"\"";
        private const string BreakString = "\n\t\t\t\t\tbreak;";
        private const string ThrowString = "throw new Exception(\"Unexpected Case\");";
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
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var switchCaseStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<SwitchStatementSyntax>().First();
            var switchCaseVariable = ((VariableDeclarationSyntax)((LocalDeclarationStatementSyntax)(
                                        ((BlockSyntax)switchCaseStatement.Parent).Statements[0])).Declaration).Variables[0];
            var sections = new List<SwitchSectionSyntax>();
            var switchCaseLabel = new SyntaxList<SwitchLabelSyntax>();
            var breakStatement = new SyntaxList<StatementSyntax>();

            switchCaseLabel = switchCaseLabel.Add(SyntaxFactory.CaseSwitchLabel(SyntaxFactory.ParseExpression(EmptyString)));
            breakStatement = breakStatement.Add(SyntaxFactory.ParseStatement(BreakString));

            sections.Add(SyntaxFactory.SwitchSection().WithLabels(switchCaseLabel).WithStatements(breakStatement));
            sections.Add(CreateSection(SyntaxFactory.DefaultSwitchLabel(), SyntaxFactory.ParseStatement(ThrowString)));

            var switchExpression = SyntaxFactory.ParseExpression(switchCaseVariable.Identifier.ValueText);
            var newsSwitchCaseStatement = SyntaxFactory.SwitchStatement(switchExpression).WithSections(new SyntaxList<SwitchSectionSyntax>().AddRange(sections));
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
