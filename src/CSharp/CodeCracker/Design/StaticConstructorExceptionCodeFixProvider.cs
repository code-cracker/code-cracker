using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, nameof(StaticConstructorExceptionCodeFixProvider)), Shared]
    public class StaticConstructorExceptionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.StaticConstructorException.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var sourceSpan = diagnostic.Location.SourceSpan;
            var @throw = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<ThrowStatementSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create("Remove this exception", ct => RemoveThrowAsync(context.Document, @throw, ct)), diagnostic);
        }

        private async Task<Document> RemoveThrowAsync(Document document, ThrowStatementSyntax @throw, CancellationToken ct)
        {
            return document.WithSyntaxRoot((await document.GetSyntaxRootAsync(ct)).RemoveNode(@throw, SyntaxRemoveOptions.KeepNoTrivia));
        }
    }
}