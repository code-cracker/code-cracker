using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringFormatAnalyzer : DiagnosticAnalyzer
    {
        internal const string Category = SupportedCategories.Style;
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.StringFormatAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.StringFormatAnalyzer_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.StringFormatAnalyzer_Description), Resources.ResourceManager, typeof(Resources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.StringFormat.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.StringFormat));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeFormatInvocation, SyntaxKind.InvocationExpression);

        private static void AnalyzeFormatInvocation(SyntaxNodeAnalysisContext context) =>
            AnalyzeFormatInvocation(context, "Format", "string.Format(string, ", "string.Format(string, params object[])", Rule);

        public static void AnalyzeFormatInvocation(SyntaxNodeAnalysisContext context, string methodName, string methodOverloadSignature, string methodWithArraySignature, DiagnosticDescriptor rule)
        {
            if (context.IsGenerated()) return;
            var invocationExpression = (InvocationExpressionSyntax)context.Node;
            var memberExpresion = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberExpresion?.Name?.ToString() != methodName) return;
            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberExpresion).Symbol;
            if (memberSymbol == null) return;
            if (!memberSymbol.ToString().StartsWith(methodOverloadSignature)) return;
            var argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
            if (argumentList?.Arguments.Count < 2) return;
            if (!argumentList.Arguments[0]?.Expression?.IsKind(SyntaxKind.StringLiteralExpression) ?? false) return;
            if (memberSymbol.ToString() == methodWithArraySignature && argumentList.Arguments.Skip(1).Any(a => context.SemanticModel.GetTypeInfo(a.Expression).Type.TypeKind == TypeKind.Array)) return;
            var formatLiteral = (LiteralExpressionSyntax)argumentList.Arguments[0].Expression;
            var format = (string)context.SemanticModel.GetConstantValue(formatLiteral).Value;
            var formatArgs = Enumerable.Range(1, argumentList.Arguments.Count - 1).Select(i => new object()).ToArray();
            try
            {
                string.Format(format, formatArgs);
            }
            catch (FormatException)
            {
                return;
            }
            var diag = Diagnostic.Create(rule, invocationExpression.GetLocation());
            context.ReportDiagnostic(diag);
        }
    }
}
