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
    [ExportCodeFixProvider("CodeCrackerRemoveWhereWhenItIsPossibleCodeFixProvider", LanguageNames.CSharp), Shared]
    public class RemoveWhereWhenItIsPossibleCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.RemoveWhereWhenItIsPossible.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var whereInvoke = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var nextMethodInvoke = whereInvoke.Parent.FirstAncestorOrSelf<InvocationExpressionSyntax>();
            var message = "Remove 'Where' moving predicate to '" + RemoveWhereWhenItIsPossibleAnalyzer.GetNameOfTheInvokedMethod(nextMethodInvoke) + "'";
            context.RegisterFix(CodeAction.Create(message, c => RemoveWhereAsync(context.Document, whereInvoke, nextMethodInvoke, c)), diagnostic);
        }

        private async Task<Document> RemoveWhereAsync(Document document, InvocationExpressionSyntax whereInvoke, InvocationExpressionSyntax nextMethodInvoke, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
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