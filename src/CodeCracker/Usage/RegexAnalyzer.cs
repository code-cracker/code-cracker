using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RegexAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0010";
        internal const string Title = "Your Regex expression is wrong";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Naming;
        const string Description = "This diagnostic compile the Regex expression and trigger if the compilation fail "
            + "by throwing an exception.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
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