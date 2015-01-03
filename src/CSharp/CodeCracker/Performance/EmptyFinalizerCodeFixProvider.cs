using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Performance
{
    [ExportCodeFixProvider("CodeCrackerEmptyFinalizerCodeFixProvider", LanguageNames.CSharp)]
    public class EmptyFinalizerCodeFixProvider : CodeFixProvider
    {

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(EmptyFinalizerAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var sourceSpan = diagnostic.Location.SourceSpan;
            var finalizer = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<DestructorDeclarationSyntax>().First();

            context.RegisterFix(
                CodeAction.Create("Remove finalizer", ct => RemoveThrow(context.Document, finalizer, ct)), diagnostic);
        }

        private async Task<Document> RemoveThrow(Document document, DestructorDeclarationSyntax finalizer, CancellationToken ct)
        {
            return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(ct)).RemoveNode(finalizer, SyntaxRemoveOptions.KeepNoTrivia));
        }
    }
}