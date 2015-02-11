using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider("CodeCrackerRemoveCommentedCodeCodeFixProvider", LanguageNames.CSharp), Shared]
    public class RemoveCommentedCodeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.RemoveCommentedCode.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var start = diagnostic.Location.SourceSpan.Start;
            context.RegisterFix(CodeAction.Create(
                "Remove commented code.",
                c => RemoveCommentedCodeAsync(context.Document, start, c)),
                diagnostic);
        }

        private async Task<Document> RemoveCommentedCodeAsync(
            Document document,
            int start,
            CancellationToken cancellationToken
            )
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var firstComment = root.FindTrivia(start);

            var codeToRemove = RemoveCommentedCodeAnalyzer.GetFullCommentedCode(root, firstComment);

            var newRoot = root;

            for (var i = 0; i < codeToRemove.NumberOfComments; i++)
            {
                var comment = newRoot.FindTrivia(start);
                newRoot = newRoot.ReplaceTrivia(comment, SyntaxTriviaList.Empty);

                var eol = newRoot.FindTrivia(start);
                newRoot = newRoot.ReplaceTrivia(eol, SyntaxTriviaList.Empty);

                var previousSpace = newRoot.FindTrivia(start - 1);
                newRoot = newRoot.ReplaceTrivia(previousSpace, SyntaxTriviaList.Empty);

            }

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}