using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NumericLiteralAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Change numeric literal expression";
        internal const string Message = "You may change {0} to a {1} literal type.";
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.NumericLiteral.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.NumericLiteral));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.NumericLiteralExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var literal = context.Node as LiteralExpressionSyntax;
            if (literal == null) return;
            var text = literal.Token.Text;
            if (!text.StartsWith("0X", System.StringComparison.OrdinalIgnoreCase)
                && (text.EndsWithAny("F", "f", "D", "d", "M", "m") || text.Contains(".") || text.IndexOfAny(new [] { 'e', 'E' }) != -1)) return;
            var newLiteralType = literal.Token.ValueText == literal.Token.Text ? "hexadecimal" : "decimal";
            var diagnostic = Diagnostic.Create(Rule, literal.GetLocation(), literal.Token.Text, newLiteralType);
            context.ReportDiagnostic(diagnostic);
        }
    }
}