using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddBracesToSwitchSectionsAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Add braces to switch sections.";
        internal const string MessageFormat = "Add braces for each section in this switch";
        internal const string Category = SupportedCategories.Refactoring;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId.AddBracesToSwitchSections));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SwitchStatement);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var @switch = (SwitchStatementSyntax) context.Node;
            if (!@switch.Sections.All(HasBraces))
                context.ReportDiagnostic(Diagnostic.Create(Rule, @switch.GetLocation()));
        }

        internal static bool HasBraces(SwitchSectionSyntax section)
        {
            if (section.Statements.Count == 1)
            {
                var firstStatement = section.Statements.First();
                if (firstStatement is BlockSyntax)
                    return true;
            }
            else if (section.Statements.Count == 2)
            {
                var firstStatement = section.Statements.First();
                var lastStatement = section.Statements.Last();
                if (firstStatement is BlockSyntax && lastStatement is BreakStatementSyntax)
                    return true;
            }
            return false;
        }
    }
}