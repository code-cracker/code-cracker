using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AbstractClassShouldNotHavePublicCtorsCodeFixProvider)), Shared]

    public class AbstractClassShouldNotHavePublicCtorsCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.AbstractClassShouldNotHavePublicCtors.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Use 'protected' instead of 'public'", c => ReplacePublicWithProtectedAsync(context.Document, diagnostic, c), nameof(AbstractClassShouldNotHavePublicCtorsCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> ReplacePublicWithProtectedAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var ctor = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();
            var @public = ctor.Modifiers.First(m => m.IsKind(SyntaxKind.PublicKeyword));
            var @protected = SyntaxFactory.Token(@public.LeadingTrivia, SyntaxKind.ProtectedKeyword,
                @public.TrailingTrivia);
            var newModifiers = ctor.Modifiers.Replace(@public, @protected);
            var newCtor = ctor.WithModifiers(newModifiers);
            var newRoot = root.ReplaceNode(ctor, newCtor);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}