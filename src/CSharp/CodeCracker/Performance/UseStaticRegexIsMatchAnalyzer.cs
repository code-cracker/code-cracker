using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStaticRegexIsMatchAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use of Regex.IsMatch might be improved";
        internal const string MessageFormat = "Use of Regex.IsMatch might be improved";
        internal const string Category = SupportedCategories.Performance;
        const string Description = "Instantiating the Regex object multiple times might be bad for performance. "
            + "You may want to use the static IsMatch method from Regex class and/or compile the regex.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.UseStaticRegexIsMatch.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.UseStaticRegexIsMatch));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var memberExpression = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberExpression?.Name.ToString() != "IsMatch") return;

            var methodSymbol = context.SemanticModel.GetSymbolInfo(memberExpression).Symbol;
            if (methodSymbol?.ContainingType.ToString() != "System.Text.RegularExpressions.Regex" || methodSymbol.IsStatic) return;
            if (!(memberExpression.Expression is IdentifierNameSyntax)) return;
            var variableSymbol = context.SemanticModel.GetSymbolInfo(((IdentifierNameSyntax)memberExpression.Expression).Identifier.Parent).Symbol;
            if (variableSymbol?.Kind != SymbolKind.Local) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocationExpression.GetLocation()));
        }
    }
}