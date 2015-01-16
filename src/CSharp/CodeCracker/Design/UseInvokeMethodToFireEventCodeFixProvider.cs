using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Design
{
    [ExportCodeFixProvider("CodeCrackerUseInvokeMethodToFireEventCodeFixProvider", LanguageNames.CSharp)]
    public class UseInvokeMethodToFireEventCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(UseInvokeMethodToFireEventAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var sourceSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            context.RegisterFix(
                CodeAction.Create("Use ?.Invoke operator and method to fire an event.", ct => UseInvoke(context.Document, invocation, ct)), diagnostic);
        }

        private async Task<Document> UseInvoke(Document document, InvocationExpressionSyntax invocation, CancellationToken ct)
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