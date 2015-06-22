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

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyObjectInitializerCodeFixProvider)), Shared]
    public class EmptyObjectInitializerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.EmptyObjectInitializer.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Remove empty object initializer", ct => RemoveAsync(context.Document, diagnostic, ct)), diagnostic);
            return Task.FromResult(0);
        }

        public static async Task<Document> RemoveAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var oldDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();
            var newDeclaration = oldDeclaration.WithInitializer(null).WithoutTrailingTrivia();
            if (newDeclaration.ArgumentList == null)
                newDeclaration = newDeclaration.WithoutTrailingTrivia().WithArgumentList(SyntaxFactory.ArgumentList());
            root = root.ReplaceNode(oldDeclaration, newDeclaration);
            var newDocument = document.WithSyntaxRoot(root);
            return newDocument;
        }
    }
}