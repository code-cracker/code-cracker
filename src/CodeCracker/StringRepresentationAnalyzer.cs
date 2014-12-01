using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker
{
    /// <summary>
    /// This analyzer produce 2 different hidden diagnostics one for regular string literals like
    /// "Hello" and another for verbatim string literalis like @"Hello".
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringRepresentationAnalyzer : DiagnosticAnalyzer
    {
        public const string RegularStringId = "CC4200";
        public const string VerbatimStringId = "CC4201";

        internal static DiagnosticDescriptor RegularStringRule = new DiagnosticDescriptor(
            RegularStringId,
            "Regular string",
            "Regular string",
            "Style",
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true);

        internal static DiagnosticDescriptor VerbatimStringRule = new DiagnosticDescriptor(
            VerbatimStringId,
            "Verbatim string",
            "Verbatim string",
            "Style",
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; }
            = ImmutableArray.Create(RegularStringRule, VerbatimStringRule);

        public override void Initialize(AnalysisContext context)
            => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.StringLiteralExpression);

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var literalExpression = (LiteralExpressionSyntax)context.Node;
            var isVerbatim = literalExpression.Token.Text.Length > 0
                && literalExpression.Token.Text.StartsWith("@\"");

            context.ReportDiagnostic(
                Diagnostic.Create(
                    isVerbatim ? VerbatimStringRule : RegularStringRule,
                    literalExpression.GetLocation()));
        }
    }
}