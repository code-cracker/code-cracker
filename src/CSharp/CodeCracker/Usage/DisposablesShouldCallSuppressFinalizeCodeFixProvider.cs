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

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider("CodeCrackerDisposablesShouldCallSuppressFinalizeCodeFixProvider", LanguageNames.CSharp), Shared]
    public class DisposablesShouldCallSuppressFinalizeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var sourceSpan = diagnostic.Location.SourceSpan;
            var method = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            context.RegisterFix(
                CodeAction.Create("Call GC.SuppressFinalize", ct => RemoveThrowAsync(context.Document, method, ct)), diagnostic);
        }

        private async Task<Document> RemoveThrowAsync(Document document, MethodDeclarationSyntax method, CancellationToken ct)
        {
            return document
                .WithSyntaxRoot((await document.GetSyntaxRootAsync(ct))
                .ReplaceNode(method, method.AddBodyStatements(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                SyntaxFactory.IdentifierName("GC"),
                                SyntaxFactory.IdentifierName("SuppressFinalize")),
                            SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.Argument(SyntaxFactory.ThisExpression())))))
                    .WithAdditionalAnnotations(Formatter.Annotation)));
        }
    }
}