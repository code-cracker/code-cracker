﻿using Microsoft.CodeAnalysis;
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

namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseInvokeMethodToFireEventCodeFixProvider)), Shared]
    public class UseInvokeMethodToFireEventCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var sourceSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create("Use ?.Invoke operator and method to fire an event.", ct => UseInvokeAsync(context.Document, invocation, ct)), diagnostic);
        }

        private async Task<Document> UseInvokeAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken ct)
        {
            var newInvocation =
                    SyntaxFactory.ConditionalAccessExpression(
                        (IdentifierNameSyntax)invocation.Expression,
                        SyntaxFactory.Token(SyntaxKind.QuestionToken),
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberBindingExpression(
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName("Invoke")),
                                invocation.ArgumentList))
                    .WithAdditionalAnnotations(Formatter.Annotation);

            return document
                        .WithSyntaxRoot((await document.GetSyntaxRootAsync(ct))
                        .ReplaceNode(invocation, newInvocation).WithTrailingTrivia(invocation.GetTrailingTrivia()));
        }
    }
}