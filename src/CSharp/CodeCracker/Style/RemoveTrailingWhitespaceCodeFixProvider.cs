using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CodeCracker.Style
{
    [ExportCodeFixProvider("CodeCrackerRemoveTrailingWhitespaceCodeFixProvider", LanguageNames.CSharp), Shared]
    public class RemoveTrailingWhitespaceCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() => ImmutableArray.Create(DiagnosticId.RemoveTrailingWhitespace.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var trivia = root.FindTrivia(diagnostic.Location.SourceSpan.End - 1);
            var newRoot = RemoveTrailingWhiteSpace(root, trivia);
            context.RegisterFix(CodeAction.Create("Remove trailing whitespace", context.Document.WithSyntaxRoot(newRoot)), diagnostic);
        }

        private static SyntaxNode RemoveTrailingWhiteSpace(SyntaxNode root, SyntaxTrivia trivia)
        {
            SyntaxNode newRoot;
            if (trivia.IsKind(SyntaxKind.WhitespaceTrivia))
            {
                newRoot = root.ReplaceTrivia(trivia, new SyntaxTrivia[] { });
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