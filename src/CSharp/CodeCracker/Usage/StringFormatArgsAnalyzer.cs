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
        internal const string MessageFormat = "The number of arguments in String.Format is incorrect.";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "The format argument in String.Format determines the number of argument, considering the {} inside. You should pass the correct number of arguments.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.StringFormatArgs.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.StringFormatArgs));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            var memberExpresion = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberExpresion?.Name?.ToString() != "Format") return;
            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberExpresion).Symbol;
            if (memberSymbol == null) return;
            if (!memberSymbol.ToString().StartsWith("string.Format(string, ")) return;
            var argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
            if (argumentList?.Arguments.Count < 2) return;
            if (!argumentList.Arguments[0]?.Expression?.IsKind(SyntaxKind.StringLiteralExpression) ?? false) return;
            if (memberSymbol.ToString() == "string.Format(string, params object[])" && argumentList.Arguments.Skip(1).Any(a => context.SemanticModel.GetTypeInfo(a.Expression).Type.TypeKind == TypeKind.Array)) return;
            var formatLiteral = (LiteralExpressionSyntax)argumentList.Arguments[0].Expression;
            var analyzingInterpolation = (InterpolatedStringExpressionSyntax)SyntaxFactory.ParseExpression($"${formatLiteral.Token.Text}");
            var allInterpolations = analyzingInterpolation.Contents.Where(c => c.IsKind(SyntaxKind.Interpolation)).Select(c => (InterpolationSyntax)c);
            if (allInterpolations.Select(c => c.Expression.ToString()).Distinct().Count() == argumentList.Arguments.Count - 1) return;
            var diag = Diagnostic.Create(Rule, invocationExpression.GetLocation());
            context.ReportDiagnostic(diag);
        }
    }
}