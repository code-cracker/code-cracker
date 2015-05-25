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

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(nameof(RemovePrivateMethodNeverUsedCodeFixProvider), LanguageNames.CSharp), Shared]
    public class RemovePrivateMethodNeverUsedCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.RemovePrivateMethodNeverUsed.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();

            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var methodNotUsed = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            context.RegisterCodeFix(CodeAction.Create($"Remove unused private method : '{methodNotUsed.Identifier.Text}'", c => RemoveMethodAsync(context.Document, methodNotUsed, c)), diagnostic);
        }

        private async Task<Document> RemoveMethodAsync(Document document, MethodDeclarationSyntax methodNotUsed, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = root.RemoveNode(methodNotUsed, SyntaxRemoveOptions.KeepNoTrivia);

            return document.WithSyntaxRoot(newRoot);
        }
    }
}