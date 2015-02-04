using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddBracesToSwitchCaseAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0071";
        internal const string Title = "Add braces to switch case.";
        internal const string MessageFormat = "Add braces for this case";
        internal const string Category = SupportedCategories.Refactoring;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(CodeCracker.DiagnosticId.AddBracesToSwitchCase));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.CaseSwitchLabel);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var caseLabel = (CaseSwitchLabelSyntax) context.Node;
            var section = caseLabel.Parent as SwitchSectionSyntax;
            if (section == null)
                return;
            
            // Only for the first case of a section
            if (section.Labels.First() != caseLabel)
                return;

            if (section.Statements.Count == 1)
            {
                // If the section already contains only a block, don't offer to refactor.
                // Example:
                //
                // case 42:
                //     {
                //         Foo();
                //         break;
                //     }

                var firstStatement = section.Statements.First();
                if (firstStatement is BlockSyntax)
                    return;
            }
            else if (section.Statements.Count == 2)
            {
                // If the section contains only a block followed by a break, don't offer to refactor.
                // Example:
                //
                // case 42:
                //     {
                //         Foo();
                //     }
                //     break;

                var firstStatement = section.Statements.First();
                var lastStatement = section.Statements.Last();
                if (firstStatement is BlockSyntax && lastStatement is BreakStatementSyntax)
                    return;
            }
            context.ReportDiagnostic(Diagnostic.Create(Rule, caseLabel.GetLocation()));
        }
    }
}
