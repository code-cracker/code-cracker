using System;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Text.RegularExpressions;

namespace CodeCracker.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveTrailingWhitespaceAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0065";
        internal const string Title = "Remove trailing whitespace";
        internal const string MessageFormat = Title;
        internal const string Category = SupportedCategories.Style;
        const string Description = "Trailing whitespaces are ugly and show sloppiness. Remove them.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));


        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxTreeAction(AnalyzeTrailingTrivia);

        private void AnalyzeTrailingTrivia(SyntaxTreeAnalysisContext context)
        {
            SourceText text;
            if (!context.Tree.TryGetText(out text)) return;
            foreach (var line in text.Lines)
            {
                if (line.End == 0) continue;
                var candidateWhiteSpace = line.Text.GetSubText(TextSpan.FromBounds(line.End - 1, line.End)).ToString();
                if (string.Compare(candidateWhiteSpace, "\n", StringComparison.Ordinal) == 0
                    || !Regex.IsMatch(candidateWhiteSpace, @"\s")) continue;
                var diag = Diagnostic.Create(Rule, Location.Create(context.Tree, line.Span));
                context.ReportDiagnostic(diag);
            }
        }
    }
}