using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringRepresentationAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly DiagnosticDescriptor RegularStringRule = new DiagnosticDescriptor(
            DiagnosticId.StringRepresentation_RegularString.ToDiagnosticId(),
            "Regular string",
            "Change to regular string",
            SupportedCategories.Refactoring,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.StringRepresentation_RegularString));

        internal static readonly DiagnosticDescriptor VerbatimStringRule = new DiagnosticDescriptor(
            DiagnosticId.StringRepresentation_VerbatimString.ToDiagnosticId(),
            "Verbatim string",
            "Change to verbatim string",
            SupportedCategories.Refactoring,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.StringRepresentation_VerbatimString));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RegularStringRule, VerbatimStringRule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.StringLiteralExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var literalExpression = (LiteralExpressionSyntax)context.Node;
            var isVerbatim = literalExpression.Token.Text.Length > 0
                && literalExpression.Token.Text.StartsWith("@\"");

            var properties = new Dictionary<string, string>
            {
                { nameof(isVerbatim), isVerbatim ? "1" : "0" },
                { "truncatedString", Truncate((string)literalExpression.Token.Value, 20) }
            }.ToImmutableDictionary();
            context.ReportDiagnostic(Diagnostic.Create(
                    isVerbatim ? VerbatimStringRule : RegularStringRule,
                    literalExpression.GetLocation(),
                    properties));
        }

        private static string Truncate(string text, int length)
        {
            var normalized = new string(text.Cast<char>().Where(c => !char.IsControl(c)).ToArray());
            return normalized.Length <= length
                ? normalized
                : normalized.Substring(0, length - 1) + "\u2026";
        }
    }
}