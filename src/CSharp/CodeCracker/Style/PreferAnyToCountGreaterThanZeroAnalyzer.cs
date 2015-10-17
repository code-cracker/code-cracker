using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class PreferAnyToCountGreaterThanZeroAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.PreferAnyToCountGreaterThanZeroAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.PreferAnyToCountGreaterThanZeroAnalyzer_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Style;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.PreferAnyToCountGreaterThanZero.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.PreferAnyToCountGreaterThanZero));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.GreaterThanExpression);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var binExpression = context.Node as BinaryExpressionSyntax;
            if (binExpression.IsNotKind(SyntaxKind.GreaterThanExpression)) return;
            var rightSideExpression = binExpression.Right as LiteralExpressionSyntax;
            if (rightSideExpression == null) return;
            if (rightSideExpression.IsNotKind(SyntaxKind.NumericLiteralExpression)) return;
            if (rightSideExpression.Token.ToString() != "0") return;
            if (binExpression.Left == null) return;
            if (binExpression.Left.IsNotKind(SyntaxKind.InvocationExpression, SyntaxKind.SimpleMemberAccessExpression)) return;
            var memberExpression = ((binExpression.Left as InvocationExpressionSyntax)?.Expression ?? binExpression.Left) as MemberAccessExpressionSyntax;
            if (memberExpression == null) return;
            if (memberExpression.Name.ToString() != "Count") return;
            var memberSymbolInfo = context.SemanticModel.GetSymbolInfo(memberExpression);
            var namespaceName = memberSymbolInfo.Symbol?.ContainingNamespace?.ToString();
            if (namespaceName != "System.Linq" && namespaceName != "System.Collections" && namespaceName != "System.Collections.Generic") return;
            context.ReportDiagnostic(Diagnostic.Create(Rule, binExpression.GetLocation(), GetPredicateString(binExpression.Left)));
        }

        private static string GetPredicateString(ExpressionSyntax expression)
        {
            var predicateString = "";
            if (expression.IsKind(SyntaxKind.InvocationExpression))
            {
                var arguments = ((InvocationExpressionSyntax)expression).ArgumentList;
                predicateString = arguments?.Arguments == null ? "" : arguments.Arguments.Count > 0 ? "predicate" : "";
            }
            return predicateString;
        }
    }
}