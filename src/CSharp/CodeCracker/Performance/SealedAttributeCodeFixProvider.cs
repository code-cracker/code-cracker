using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SealedAttributeCodeFixProvider)), Shared]
    public class SealedAttributeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.SealedAttribute.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Mark as sealed", ct => MarkClassAsSealedAsync(context.Document, diagnostic, ct)), diagnostic);
            return Task.FromResult(0);
        }

        private async Task<Document> MarkClassAsSealedAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var sourceSpan = diagnostic.Location.SourceSpan;
            var type = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
            return document
                .WithSyntaxRoot(root
                .ReplaceNode(
                    type,
                    type
                        .WithModifiers(type.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
                        .WithAdditionalAnnotations(Formatter.Annotation)));
        }
    }
}