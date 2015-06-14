using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveWhereWhenItIsPossibleCodeFixProvider)), Shared]
    public class RemoveWhereWhenItIsPossibleCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.RemoveWhereWhenItIsPossible.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var name = diagnostic.Properties["methodName"];
            var message = $"Remove 'Where' moving predicate to '{name}'";
            context.RegisterCodeFix(CodeAction.Create(message, c => RemoveWhereAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async Task<Document> RemoveWhereAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var whereInvoke = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var nextMethodInvoke = whereInvoke.Parent.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var whereMemberAccess = whereInvoke.ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            var nextMethodMemberAccess = nextMethodInvoke.ChildNodes().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            var newNextMethodInvoke = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, whereMemberAccess.Expression, nextMethodMemberAccess.Name),
                whereInvoke.ArgumentList);
            var newRoot = root.ReplaceNode(nextMethodInvoke, newNextMethodInvoke);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}