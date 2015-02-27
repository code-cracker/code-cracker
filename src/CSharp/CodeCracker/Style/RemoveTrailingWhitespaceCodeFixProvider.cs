using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider("CodeCrackerRemoveTrailingWhitespaceCodeFixProvider", LanguageNames.CSharp), Shared]
    public class RemoveTrailingWhitespaceCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.RemoveTrailingWhitespace.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var trivia = root.FindTrivia(diagnostic.Location.SourceSpan.End - 1);
            var newRoot = RemoveTrailingWhiteSpace(root, trivia);
            context.RegisterCodeFix(CodeAction.Create("Remove trailing whitespace", ct => Task.FromResult(context.Document.WithSyntaxRoot(newRoot))), diagnostic);
        }

        private static SyntaxNode RemoveTrailingWhiteSpace(SyntaxNode root, SyntaxTrivia trivia)
        {
            SyntaxNode newRoot;
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                newRoot = root.ReplaceTrivia(trivia, new SyntaxTrivia[] { });
            }
            else if (trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia, SyntaxKind.MultiLineDocumentationCommentTrivia))
            {
                var commentText = trivia.ToFullString();
                var commentLines = commentText.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
                var newComment = "";
                var builder = new System.Text.StringBuilder();
                builder.Append(newComment);
                for (int i = 0; i < commentLines.Length; i++)
                {
                    var commentLine = commentLines[i];
                    builder.Append(Regex.Replace(commentLine, @"\s+$", ""));
                    if (i < commentLines.Length - 1) builder.Append(Environment.NewLine);
                }
                newComment = builder.ToString();
                newRoot = root.ReplaceTrivia(trivia, SyntaxFactory.SyntaxTrivia(SyntaxKind.DocumentationCommentExteriorTrivia, newComment));
            }
            else
            {
                var triviaNoTrailingWhiteSpace = Regex.Replace(trivia.ToFullString(), @"\s+$", "");
                newRoot = root.ReplaceTrivia(trivia, SyntaxFactory.ParseTrailingTrivia(triviaNoTrailingWhiteSpace));
            }
            return newRoot;
        }
    }
}