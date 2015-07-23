using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ChangeAsToCastCodeFixProvider)), Shared]
    public class ChangeAsToCastCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.ChangeAsToCast.ToDiagnosticId());

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            if (node.Kind() == SyntaxKind.AsExpression)
                context.RegisterCodeFix(CodeAction.Create("Change safe cast ('as') to direct cast", ct => ChangeAsToCastAsync(context.Document, root, node)), diagnostic);
            else if (node.Kind() == SyntaxKind.CastExpression)
                context.RegisterCodeFix(CodeAction.Create("Change direct cast to safe cast ('as')", ct => ChangeCastToAsAsync(context.Document, root, node)), diagnostic);
        }

        private static Task<Document> ChangeAsToCastAsync(Document document, SyntaxNode root, SyntaxNode node)
        {
            var @as = (BinaryExpressionSyntax) node;
            var type = (TypeSyntax) @as.Right;
            var cast = SyntaxFactory.CastExpression(type, @as.Left).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(@as, cast);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private static Task<Document> ChangeCastToAsAsync(Document document, SyntaxNode root, SyntaxNode node)
        {
            var cast = (CastExpressionSyntax) node;
            var @as = SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, cast.Expression, cast.Type).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(cast, @as);
            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }
    }
}