using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnnecessaryParenthesisCodeFixProvider)), Shared]
    public class UnnecessaryToStringInStringConcatenationCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.UnnecessaryToStringInStringConcatenation.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Remove unnecessary ToString", ct => RemoveUnnecessaryToStringAsync(context.Document, diagnostic, ct), nameof(UnnecessaryToStringInStringConcatenationCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }
        private static async Task<Document> RemoveUnnecessaryToStringAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var invocationExpression = 
                root
                .FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent
                .AncestorsAndSelf()
                .OfType<InvocationExpressionSyntax>()
                .First();

            var onlyMemberAccessNode = 
                invocationExpression
                .ChildNodes()
                .First()
                .ChildNodes()
                .First();

            var newRoot = root.ReplaceNode(invocationExpression, onlyMemberAccessNode);

            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

    }

}