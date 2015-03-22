using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System;
using System.Reflection;

namespace CodeCracker.CSharp.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStaticRegexIsMatchAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use static Regex.IsMatch";
        internal const string MessageFormat = "Using static Regex.IsMatch can give better performance";
        internal const string Category = SupportedCategories.Performance;
        const string Description = "Instantiating the Regex object multiple times is bad for performance. "
            + "Prefer using the static IsMatch method from Regex class";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
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

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var memberExpresion = invocationExpression.Expression as MemberAccessExpressionSyntax;

            if (memberExpresion?.Name.ToString() != "IsMatch") return;

            var methodSymbol = context.SemanticModel.GetSymbolInfo(memberExpresion).Symbol;
            var variableSymbol = context.SemanticModel.GetSymbolInfo(((IdentifierNameSyntax)memberExpresion.Expression).Identifier.Parent).Symbol;

            if (methodSymbol == null) return;
            if (variableSymbol == null) return;

            if (methodSymbol.ContainingType.ToString() != "System.Text.RegularExpressions.Regex") return;

            if (methodSymbol.IsStatic) return;

            if (variableSymbol.Kind != SymbolKind.Local) return;
            
            context.ReportDiagnostic(Diagnostic.Create(Rule, invocationExpression.GetLocation()));
        }
    }
}