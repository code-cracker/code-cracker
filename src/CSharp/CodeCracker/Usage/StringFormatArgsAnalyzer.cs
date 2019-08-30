using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringFormatArgsAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Incorrect String.Format usage";
        internal const string IncorrectNumberOfArgsMessage = "The number of arguments in String.Format is incorrect.";
        internal const string InvalidArgsReferenceMessage = "Invalid argument reference in String.Format.";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "The format argument in String.Format determines the number of other arguments that need to be "
            + "passed into the method based on the number of curly braces {} used. The incorrect number of arguments are being passed.";

        internal static readonly DiagnosticDescriptor ExtraArgs = new DiagnosticDescriptor(
            DiagnosticId.StringFormatArgs_ExtraArgs.ToDiagnosticId(),
            Title,
            IncorrectNumberOfArgsMessage,
            Category,
            SeverityConfigurations.Current[DiagnosticId.StringFormatArgs_ExtraArgs],
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.StringFormatArgs_ExtraArgs));

        internal static readonly DiagnosticDescriptor InvalidArgs = new DiagnosticDescriptor(
            DiagnosticId.StringFormatArgs_InvalidArgs.ToDiagnosticId(),
            Title,
            InvalidArgsReferenceMessage,
            Category,
            SeverityConfigurations.Current[DiagnosticId.StringFormatArgs_InvalidArgs],
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.StringFormatArgs_InvalidArgs));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(ExtraArgs, InvalidArgs);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            var memberExpresion = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberExpresion?.Name?.ToString() != "Format") return;
            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberExpresion).Symbol;
            if (memberSymbol == null) return;
            var memberSignature = memberSymbol.ToString();
            if (!memberSignature.StartsWith("string.Format(string, ")) return;
            var argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
            if (argumentList == null) return;
            var arguments = argumentList.Arguments;
            if (!arguments[0]?.Expression?.IsKind(SyntaxKind.StringLiteralExpression) ?? false) return;
            if (memberSignature == "string.Format(string, params object[])" && arguments.Count == 2 && context.SemanticModel.GetTypeInfo(arguments[1].Expression).Type.TypeKind == TypeKind.Array) return;
            var formatLiteral = (LiteralExpressionSyntax)arguments[0].Expression;
            var analyzingInterpolation = (InterpolatedStringExpressionSyntax)SyntaxFactory.ParseExpression($"${formatLiteral.Token.Text}");
            var allInterpolations = analyzingInterpolation.Contents.Where(c => c.IsKind(SyntaxKind.Interpolation)).Select(c => (InterpolationSyntax)c);
            var distinctInterpolations = allInterpolations.Select(c => c.Expression.ToString()).Distinct();
            if (distinctInterpolations.Count() < arguments.Count - 1)
            {
                var diag = Diagnostic.Create(ExtraArgs, invocationExpression.GetLocation());
                context.ReportDiagnostic(diag);
                return;
            }
            foreach (var interpolation in distinctInterpolations)
            {
                var validIndexReference = false;
                int argIndexReference;
                if (int.TryParse(interpolation, out argIndexReference))
                {
                    validIndexReference = argIndexReference >= 0 && argIndexReference < arguments.Count - 1;
                }
                if (!validIndexReference)
                {
                    var diag = Diagnostic.Create(InvalidArgs, invocationExpression.GetLocation());
                    context.ReportDiagnostic(diag);
                    return;
                }
            }
        }
    }
}
