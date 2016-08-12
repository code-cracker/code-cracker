using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ResultInAsyncCodeFixProvider)), Shared]
    public class ResultInAsyncCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ResultInAsync.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public async sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var compilation = (CSharpCompilation)await context.Document.Project.GetCompilationAsync();
            context.RegisterCodeFix(CodeAction.Create(
                Resources.ResultInAsyncCodeFixProvider_Title,
                ct => ReplaceResultWithAwaitAsync(context.Document, diagnostic, ct),
                nameof(ResultInAsyncCodeFixProvider)
               ), diagnostic);
        }

        private async static Task<Document> ReplaceResultWithAwaitAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = (await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false));
            var sourceSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var memberAccess = invocation.Parent as MemberAccessExpressionSyntax;

            // Replace memberAccess with the async invocation
            SyntaxNode newRoot;

            // See if the member access expression is a part of something bigger
            // i.e. something.Result.something. Then we need to produce (await something.Result).something
            var parentAccess = memberAccess.Parent as MemberAccessExpressionSyntax;
            if (parentAccess != null)
            {
                var rewritten =
                    SyntaxFactory.ParenthesizedExpression(
                        SyntaxFactory.AwaitExpression(
                            invocation)
                        )
                    .WithLeadingTrivia(invocation.GetLeadingTrivia())
                    .WithTrailingTrivia(invocation.GetTrailingTrivia());
                var subExpression = parentAccess.Expression;
                newRoot = root.ReplaceNode(subExpression, rewritten);
            }
            else
            {
                var rewritten =
                    SyntaxFactory.AwaitExpression(
                        invocation)
                    .WithLeadingTrivia(invocation.GetLeadingTrivia())
                    .WithTrailingTrivia(invocation.GetTrailingTrivia());
                newRoot = root.ReplaceNode(memberAccess, rewritten);
            }

            return document.WithSyntaxRoot(newRoot);
        }
    }
}