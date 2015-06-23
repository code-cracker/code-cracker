using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyFinalizerCodeFixProvider)), Shared]
    public class EmptyFinalizerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.EmptyFinalizer.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create("Remove finalizer", ct => RemoveThrowAsync(context.Document, diagnostic, ct)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> RemoveThrowAsync(Document document, Diagnostic diagnostic , CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var sourceSpan = diagnostic.Location.SourceSpan;
            var finalizer = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<DestructorDeclarationSyntax>().First();
            return document.WithSyntaxRoot(root.RemoveNode(finalizer, SyntaxRemoveOptions.KeepNoTrivia));
        }
    }
}