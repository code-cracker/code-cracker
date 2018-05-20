using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AddBracesToSwitchSectionsAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Add braces to switch sections.";
        internal const string MessageFormat = "Add braces for each section in this switch";
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.AddBracesToSwitchSections));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SwitchStatement);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var @switch = (SwitchStatementSyntax)context.Node;
            if (!@switch.Sections.All(HasBraces))
                context.ReportDiagnostic(Diagnostic.Create(Rule, @switch.GetLocation()));
        }

        internal static bool HasBraces(SwitchSectionSyntax section)
        {
            switch (section.Statements.Count)
            {
                case 1:
                    if (section.Statements.First() is BlockSyntax)
                        return true;
                    break;
                case 2:
                    if (section.Statements.First() is BlockSyntax && section.Statements.Last() is BreakStatementSyntax)
                        return true;
                    break;
                default:
                    break;
            }
            return false;
        }
    }
}