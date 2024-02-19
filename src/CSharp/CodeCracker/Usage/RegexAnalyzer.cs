using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RegexAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Your regex expression is incorrect";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Naming;
        const string Description = "There is an error in your regex expression.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.Regex.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            SeverityConfigurations.CurrentCS[DiagnosticId.Regex],
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.Regex));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocationExpression = (InvocationExpressionSyntax)context.Node;

            var memberExpresion = invocationExpression.Expression as MemberAccessExpressionSyntax;
            if (memberExpresion?.Name?.ToString() != "Match") return;

            var memberSymbol = context.SemanticModel.GetSymbolInfo(memberExpresion).Symbol;
            if (memberSymbol?.ToString() != "System.Text.RegularExpressions.Regex.Match(string, string)") return;

            var argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
            if ((argumentList?.Arguments.Count ?? 0) != 2) return;

            var regexLiteral = argumentList.Arguments[1].Expression as LiteralExpressionSyntax;
            if (regexLiteral == null) return;

            var regexOpt = context.SemanticModel.GetConstantValue(regexLiteral);

            var regex = regexOpt.Value as string;

            try
            {
                System.Text.RegularExpressions.Regex.Match("", regex);
            }
            catch (ArgumentException e)
            {
                var diag = Diagnostic.Create(Rule, regexLiteral.GetLocation(), e.Message);
                context.ReportDiagnostic(diag);
            }
        }
    }
}
