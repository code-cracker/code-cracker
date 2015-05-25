using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyObjectInitializerCodeFixProvider)), Shared]
    public class EmptyObjectInitializerCodeFixProvider : CodeFixProvider
    {
        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var oldDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();
            var newDeclaration = oldDeclaration.WithInitializer(null).WithoutTrailingTrivia();
            if (newDeclaration.ArgumentList == null)
                newDeclaration = newDeclaration.WithoutTrailingTrivia().WithArgumentList(SyntaxFactory.ArgumentList());
            root = root.ReplaceNode(oldDeclaration, newDeclaration);
            var newDocument = context.Document.WithSyntaxRoot(root);
            context.RegisterCodeFix(CodeAction.Create("Remove empty object initializer", ct => Task.FromResult(newDocument)), diagnostic);
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.EmptyObjectInitializer.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
    }
}