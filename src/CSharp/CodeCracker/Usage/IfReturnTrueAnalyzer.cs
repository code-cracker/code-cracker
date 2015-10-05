using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IfReturnTrueAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Return Condition directly";
        internal const string Message = "{0}";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "Using an if/else to return true/false depending on the condition isn't useful.\r\n"
            + "As the condition is already a boolean it can be returned directly";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.IfReturnTrue.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.IfReturnTrue));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.IfStatement);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var ifStatement = context.Node as IfStatementSyntax;
            if (ifStatement?.Else == null) return;
            var statementInsideIf = ifStatement.Statement.GetSingleStatementFromPossibleBlock();
            if (statementInsideIf == null) return;
            var statementInsideElse = ifStatement.Else.Statement.GetSingleStatementFromPossibleBlock();
            if (statementInsideElse == null) return;
            var returnIf = statementInsideIf as ReturnStatementSyntax;
            var returnElse = statementInsideElse as ReturnStatementSyntax;
            if (returnIf == null || returnElse == null) return;
            if ((returnIf.Expression is LiteralExpressionSyntax && returnIf.Expression.IsKind(SyntaxKind.TrueLiteralExpression) &&
                returnElse.Expression is LiteralExpressionSyntax && returnElse.Expression.IsKind(SyntaxKind.FalseLiteralExpression)) ||
                (returnIf.Expression is LiteralExpressionSyntax && returnIf.Expression.IsKind(SyntaxKind.FalseLiteralExpression) &&
                returnElse.Expression is LiteralExpressionSyntax && returnElse.Expression.IsKind(SyntaxKind.TrueLiteralExpression)))
            {
                var diagnostic = Diagnostic.Create(Rule, ifStatement.IfKeyword.GetLocation(), "You should return directly.");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}