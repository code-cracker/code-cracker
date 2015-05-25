using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace CodeCracker.CSharp.Reliability
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseConfigureAwaitFalseCodeFixProvider)), Shared]
    public class UseConfigureAwaitFalseCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.UseConfigureAwaitFalse.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Use ConfigureAwait(false)", ct => CreateUseConfigureAwaitAsync(context.Document, diagnostic.Location.SourceSpan, ct)), diagnostic);
            return Task.FromResult(0);
        }

        private async Task<Document> CreateUseConfigureAwaitAsync(Document document, TextSpan textSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var awaitExpression = (AwaitExpressionSyntax)root.FindNode(textSpan);
            var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    awaitExpression.Expression,
                    SyntaxFactory.IdentifierName("ConfigureAwait")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(awaitExpression.Expression, newExpression);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}