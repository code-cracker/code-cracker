using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MergeNestedIfAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Merge nested ifs";
        internal const string MessageFormat = "Merge nested ifs into a single if";
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.MergeNestedIf.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.MergeNestedIf));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IfStatement);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var ifStatement = (IfStatementSyntax)context.Node;
            var nestedIf = ifStatement?.Statement.GetSingleStatementFromPossibleBlock() as IfStatementSyntax;
            if (nestedIf == null || ifStatement.Else != null || nestedIf.Else != null) return;
            var diagnostic = Diagnostic.Create(Rule, ifStatement.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}