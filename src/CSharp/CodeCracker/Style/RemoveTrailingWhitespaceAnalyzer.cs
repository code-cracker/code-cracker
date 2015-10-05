using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveTrailingWhitespaceAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Remove trailing whitespace";
        internal const string MessageFormat = Title;
        internal const string Category = SupportedCategories.Style;
        const string Description = "Trailing whitespaces are ugly and show sloppiness. Remove them.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RemoveTrailingWhitespace.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveTrailingWhitespace));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxTreeAction(AnalyzeTrailingTrivia);

        private static void AnalyzeTrailingTrivia(SyntaxTreeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            SourceText text;
            if (!context.Tree.TryGetText(out text)) return;
            SyntaxNode root;
            if (!context.Tree.TryGetRoot(out root)) return;
            foreach (var line in text.Lines)
            {
                if (line.End == 0) continue;
                var endSpan = TextSpan.FromBounds(line.End - 1, line.End);
                var candidateWhiteSpace = line.Text.GetSubText(endSpan).ToString();
                if (string.Compare(candidateWhiteSpace, "\n", StringComparison.Ordinal) == 0
                    || !Regex.IsMatch(candidateWhiteSpace, @"\s")) continue;
                var isLiteral = root.FindNode(endSpan) is LiteralExpressionSyntax;
                if (isLiteral) return;
                var diag = Diagnostic.Create(Rule, Location.Create(context.Tree, line.Span));
                context.ReportDiagnostic(diag);
            }
        }
    }
}