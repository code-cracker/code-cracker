using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryToStringInStringConcatenationAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Unnecessary '.ToString()' call in string concatenation.";
        internal const string MessageFormat = Title;
        internal const string Category = SupportedCategories.Style;
        const string Description = "The runtime automatically calls '.ToString()' method for" +
            " string concatenation operations when there is no parameters. Remove them.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.UnnecessaryToStringInStringConcatenation.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            customTags: WellKnownDiagnosticTags.Unnecessary,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.UnnecessaryToStringInStringConcatenation));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.AddExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var addExpression = (BinaryExpressionSyntax)context.Node;

            var hasInvocationExpression = addExpression.ChildNodesAndTokens().Any(x => x.IsKind(SyntaxKind.InvocationExpression));

            //string concatenation must have an InvocationExpression
            if (!hasInvocationExpression) return;
            var invocationExpressionsThatHaveToStringCall = GetInvocationExpressionsThatHaveToStringCall(addExpression);

            foreach (var expression in invocationExpressionsThatHaveToStringCall)
            {
                var lastDot = expression.Expression.ChildNodesAndTokens().Last(x => x.IsKind(SyntaxKind.DotToken));
                var toStringTextSpan = new TextSpan(lastDot.Span.Start, expression.ArgumentList.Span.End - lastDot.Span.Start);
                var diagnostic = Diagnostic.Create(Rule, Location.Create(context.Node.SyntaxTree, toStringTextSpan));
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static IEnumerable<InvocationExpressionSyntax> GetInvocationExpressionsThatHaveToStringCall(BinaryExpressionSyntax addExpression)
        {
            return addExpression.ChildNodes().OfType<InvocationExpressionSyntax>()
                //Only default call to ToString method must be accepted
                .Where(x => x.Expression.ToString().EndsWith(@".ToString") && !x.ArgumentList.Arguments.Any());
        }
    }
}