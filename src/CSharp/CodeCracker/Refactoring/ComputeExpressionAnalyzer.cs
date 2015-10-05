using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ComputeExpressionAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Change for expression value";
        internal const string Message = "Change '{0}' for expression value";
        internal const string Category = SupportedCategories.Refactoring;
        const string Description = "You may change an expression for its value if the expression is made of literal values.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ComputeExpression.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ComputeExpression));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.AddExpression, SyntaxKind.SubtractExpression, SyntaxKind.MultiplyExpression, SyntaxKind.DivideExpression);

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var expression = context.Node as BinaryExpressionSyntax;
            if (expression == null) return;
            if (HasErrors(context.SemanticModel, expression)) return;
            var nodeToReportDiagnosticOn = expression.Parent is ParenthesizedExpressionSyntax ? expression.Parent : expression;
            if (IsPartOfALargerBinaryExpression(nodeToReportDiagnosticOn)) return;
            if (!AreLeftAndRightLiterals(expression)) return;
            var diagnostic = Diagnostic.Create(Rule, nodeToReportDiagnosticOn.GetLocation(), nodeToReportDiagnosticOn.ToString());
            context.ReportDiagnostic(diagnostic);
        }

        private static bool HasErrors(SemanticModel semanticModel, SyntaxNode node)
        {
            var diags = semanticModel.GetDiagnostics(node.Span);
            if (diags.Any(d => d.Id.StartsWith("CS"))) return true;
            return false;
        }

        private static bool IsPartOfALargerBinaryExpression(SyntaxNode nodeToReportDiagnosticOn) =>
            nodeToReportDiagnosticOn.Parent is BinaryExpressionSyntax;

        private bool AreLeftAndRightLiterals(BinaryExpressionSyntax expression)
        {
            if (!IsLiteralOrMadeOfBinaryOperations(expression.Left) ||
                !IsLiteralOrMadeOfBinaryOperations(expression.Right)) return false;
            return true;
        }

        private bool IsLiteralOrMadeOfBinaryOperations(ExpressionSyntax expression)
        {
            if (expression is BinaryExpressionSyntax)
            {
                if (!AreLeftAndRightLiterals((BinaryExpressionSyntax)expression)) return false;
            }
            else if (expression is ParenthesizedExpressionSyntax)
            {
                var binary = ((ParenthesizedExpressionSyntax)expression).Expression as BinaryExpressionSyntax;
                if (binary == null || !AreLeftAndRightLiterals(binary)) return false;
            }
            else
            {
                if (!expression.IsKind(SyntaxKind.NumericLiteralExpression)) return false;
            }
            return true;
        }
    }
}