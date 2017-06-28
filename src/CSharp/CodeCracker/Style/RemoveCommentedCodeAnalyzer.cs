using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Immutable;
using System.Linq;
using System.Text;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveCommentedCodeAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Remove commented code.";
        internal const string MessageFormat = "Commented code should be removed.";
        internal const string Category = SupportedCategories.Style;
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RemoveCommentedCode.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveCommentedCode));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxTreeAction(AnalyzeSingleLineCommentTrivia);

        private void AnalyzeSingleLineCommentTrivia(SyntaxTreeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var root = context.Tree.GetRoot();

            var comments = root.DescendantTrivia()
                .Where(trivia => trivia.IsKind(SyntaxKind.SingleLineCommentTrivia))
                .ToArray();

            for (var i = 0; i < comments.Length; i++)
            {
                var comment = comments[i];

                var code = GetFullCommentedCode(root, comment);
                if (!CouldBeSourceCode(code.Code)) continue;

                i += code.NumberOfComments - 1;

                Location.Create(context.Tree, new TextSpan(code.Start, code.End));
                var diagnostic = Diagnostic.Create(Rule, comment.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        readonly CSharpParseOptions options = new CSharpParseOptions(documentationMode: DocumentationMode.None, kind: SourceCodeKind.Script);
        bool CouldBeSourceCode(string source)
        {
            source = source.Trim();
            //single word is valid C#, but we don't want to remove that
            if (source.IndexOf(" ") == -1) return false;

            var compilation = SyntaxFactory.ParseSyntaxTree(source, options);

            var diagnostics = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();

            // class?
            if (diagnostics.Length == 2)
            {
                // missing {
                if (!diagnostics[0].Id.Equals("CS1514")) return false;
                // missing }
                if (!diagnostics[1].Id.Equals("CS1513")) return false;
                return true;
            }

            // if / while
            else if (diagnostics.Length == 1)
            {
                // missing statement
                if (!diagnostics[0].Id.Equals("CS1733")) return false;
                return true;
            }

            if (diagnostics.Length != 0) return false;

            return true;
        }

        internal static GetFullCommentedCodeResult GetFullCommentedCode(SyntaxNode root, SyntaxTrivia firstComment)
        {
            var result = new StringBuilder();
            var current = firstComment;
            var numberOfComments = 1;
            var start = firstComment.GetLocation().SourceSpan.Start;
            int end;
            do
            {
                end = current.GetLocation().SourceSpan.End;
                result.Append(current.ToString().Substring(2));

                var eol = root.FindTrivia(current.GetLocation().SourceSpan.End + 1);
                if (!eol.IsKind(SyntaxKind.EndOfLineTrivia)) break;

                var whitespace = root.FindTrivia(eol.GetLocation().SourceSpan.End + 1);
                if (!whitespace.IsKind(SyntaxKind.WhitespaceTrivia)) break;

                current = root.FindTrivia(whitespace.GetLocation().SourceSpan.End + 1);
                if (!current.IsKind(SyntaxKind.SingleLineCommentTrivia)) break;

                numberOfComments ++;

            } while (true);
            return new GetFullCommentedCodeResult(result.ToString(), numberOfComments, start, end);
        }

        internal class GetFullCommentedCodeResult
        {
            public string Code { get; }
            public int NumberOfComments{ get; }
            public int Start { get; }
            public int End { get; }

            public GetFullCommentedCodeResult(string code, int numberOfComments, int start, int end)
            {
                Code = code;
                NumberOfComments = numberOfComments;
                Start = start;
                End = end;
            }
        }
    }
}