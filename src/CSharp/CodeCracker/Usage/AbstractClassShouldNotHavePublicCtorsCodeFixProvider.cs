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
    [ExportCodeFixProvider("CodeCrackerAbstractClassShouldNotHavePublicCtorsCodeFixProvider", LanguageNames.CSharp), Shared]

    public class AbstractClassShouldNotHavePublicCtorsCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.AbstractClassShouldNotHavePublicCtors.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var ctor = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();

            context.RegisterFix(CodeAction.Create("Use 'protected' instead of 'public'", c => ReplacePublicWithProtectedAsync(context.Document, ctor, c)), diagnostic);
        }

        private async Task<Document> ReplacePublicWithProtectedAsync(Document document, ConstructorDeclarationSyntax ctor, CancellationToken cancellationToken)
        {
            var @public = ctor.Modifiers.First(m => m.IsKind(SyntaxKind.PublicKeyword));
            var @protected = SyntaxFactory.Token(@public.LeadingTrivia, SyntaxKind.ProtectedKeyword,
                @public.TrailingTrivia);

            var newModifiers = ctor.Modifiers.Replace(@public, @protected);
            var newCtor = ctor.WithModifiers(newModifiers);

            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(ctor, newCtor);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}