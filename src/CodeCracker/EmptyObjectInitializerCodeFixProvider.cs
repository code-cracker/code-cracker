using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerEmptyObjectInitializerCodeFixProvider", LanguageNames.CSharp), Shared]
    public class EmptyObjectInitializerCodeFixProvider : CodeFixProvider
    {
        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
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
            context.RegisterFix(CodeAction.Create("Remove empty object initializer", newDocument), diagnostic);
        }

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(EmptyObjectInitializerAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}