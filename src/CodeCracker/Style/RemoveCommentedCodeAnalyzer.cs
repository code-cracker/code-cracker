using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCracker.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveCommentedCodeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0037";
        internal const string Title = "Remove commented code.";
        internal const string MessageFormat = "If code is commented, it should be removed.";
        internal const string Category = SupportedCategories.Style;
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }
        // comment
        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxTreeAction(AnalyzeSingleLineCommentTrivia);
        }

        private void AnalyzeSingleLineCommentTrivia(SyntaxTreeAnalysisContext context)
        {
            var root = context.Tree.GetRoot();

            var comments = root.DescendantTrivia()
                .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia));

            foreach (var comment in comments)
            {
                var code = comment.ToString().Substring(2);
                var options = new CSharpParseOptions(kind: SourceCodeKind.Interactive, documentationMode: DocumentationMode.None);
                var compilation = SyntaxFactory.ParseSyntaxTree(code, options);
                
                var errorsCount = compilation.GetDiagnostics()
                    .Count(d => d.Severity == DiagnosticSeverity.Error);

                if (errorsCount == 0)
                {
                    var diagnostic = Diagnostic.Create(Rule, comment.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }                
        }
    }
}
