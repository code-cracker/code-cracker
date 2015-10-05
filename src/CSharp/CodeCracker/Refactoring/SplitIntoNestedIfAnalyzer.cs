using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SplitIntoNestedIfAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Split into nested if";
        internal const string Message = "Split into nested if.";
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.SplitIntoNestedIf.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.SplitIntoNestedIf));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.IfStatement);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var ifStatement = context.Node as IfStatementSyntax;
            if (!ifStatement?.Condition.IsKind(SyntaxKind.LogicalAndExpression) ?? true) return;
            if (ifStatement.Else != null) return;
            var diagnostic = Diagnostic.Create(Rule, ifStatement.Condition.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}