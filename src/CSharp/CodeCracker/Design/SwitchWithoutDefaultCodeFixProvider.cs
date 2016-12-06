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
            var switchCaseLabel = new SyntaxList<SwitchLabelSyntax>();
            var kindOfVariable = string.Empty;
            var idForVariable = string.Empty;
            var breakStatement = new SyntaxList<StatementSyntax>();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var newSections = new List<SwitchSectionSyntax>();
            var switchCaseStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<SwitchStatementSyntax>().First();
            idForVariable = ((IdentifierNameSyntax)switchCaseStatement.ChildNodes().ToList().First(n => n.Kind() == SyntaxKind.IdentifierName)).Identifier.ValueText;
            var parametersOfMethod = from init in root.DescendantNodesAndSelf()
                                     where init.Kind() == SyntaxKind.Parameter
                                     select init;
            // Verify if the variable of Switch as a same of Method Parameter 
            foreach (var parameter in parametersOfMethod)
                if (idForVariable == ((ParameterSyntax)parameter).Identifier.ValueText)
                    kindOfVariable = ((ParameterSyntax)parameter).Type.ToString();

            if (kindOfVariable == string.Empty)
            {
                var statements = ((BlockSyntax)switchCaseStatement.Parent).Statements;
                var inicializerOfSwitch = from init in statements
                                          where init.Kind() == SyntaxKind.LocalDeclarationStatement
                                          select init;
                if (inicializerOfSwitch.Any())
                {
                    var local = (LocalDeclarationStatementSyntax)inicializerOfSwitch.First();
                    var switchCaseVariable = ((local).Declaration).Variables[0];
                    kindOfVariable = ((LiteralExpressionSyntax)(switchCaseVariable.Initializer.Value)).Kind().ToString();
                }
            }

            if ((kindOfVariable.Equals("FalseLiteralExpression")) || (kindOfVariable.Equals("TrueLiteralExpression"))
                || (kindOfVariable.Equals("bool")))
            {
                var oldSections = switchCaseStatement.Sections.ToList();
                var type = string.Empty;
                foreach (var sec in oldSections)
                {
                    newSections.Add(sec);
                    type = (((CaseSwitchLabelSyntax)((SwitchSectionSyntax)sec).ChildNodes().ToList().First()).Value).GetFirstToken().Text;
                }
                var againstType = string.Empty;
                if (type.Equals("true")) againstType = "false"; else againstType = "true";

                newSections.Add(SyntaxFactory.SwitchSection().WithLabels(
                            switchCaseLabel.Add(SyntaxFactory.CaseSwitchLabel(SyntaxFactory.ParseExpression(againstType)))).
                               WithStatements(breakStatement.Add(SyntaxFactory.ParseStatement(BreakString))));
            }
            else
            {
                var oldSections = switchCaseStatement.Sections.ToList();
                foreach (var sec in oldSections)
                    newSections.Add(sec);

                breakStatement = breakStatement.Add(SyntaxFactory.ParseStatement(BreakString));
                newSections.Add(CreateSection(SyntaxFactory.DefaultSwitchLabel(), SyntaxFactory.ParseStatement(ThrowString)));
            }
            var switchExpression = SyntaxFactory.ParseExpression(idForVariable);
            var newsSwitchCaseStatement = SyntaxFactory.SwitchStatement(switchExpression).
                            WithSections(new SyntaxList<SwitchSectionSyntax>().AddRange(newSections));
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
