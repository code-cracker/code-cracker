using System.Collections.Immutable;
using System.Composition;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Linq;
using System.Threading;
using CodeCracker.Properties;
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

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            switch (diagnostic.Properties["kind"])
            {
                case "AsExpression":
                    context.RegisterCodeFix(CodeAction.Create(Resources.ChangeAsToCastCodeFixProvider_AsToCast, ct => ChangeAsToCastAsync(context.Document, diagnostic, ct), nameof(ChangeAsToCastCodeFixProvider) + "_AsToCast"), diagnostic);
                    break;
                case "CastExpression":
                    context.RegisterCodeFix(CodeAction.Create(Resources.ChangeAsToCastCodeFixProvider_CastToAs, ct => ChangeCastToAsAsync(context.Document, diagnostic, ct), nameof(ChangeAsToCastCodeFixProvider) + "_CastToAs"), diagnostic);
                    break;
            }
            return Task.FromResult(0);
        }

        private static async Task<Document> ChangeAsToCastAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var @as = (BinaryExpressionSyntax) node;
            var type = (TypeSyntax) @as.Right;
            var cast = SyntaxFactory.CastExpression(type, @as.Left).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(@as, cast);
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Document> ChangeCastToAsAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var cast = (CastExpressionSyntax) node;
            var @as = SyntaxFactory.BinaryExpression(SyntaxKind.AsExpression, cast.Expression, cast.Type).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(cast, @as);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}