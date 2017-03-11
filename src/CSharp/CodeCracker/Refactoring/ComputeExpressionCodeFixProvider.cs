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

namespace CodeCracker.CSharp.Refactoring
{

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ComputeExpressionCodeFixProvider)), Shared]
    public class ComputeExpressionCodeFixProvider : CodeFixProvider
    {

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ComputeExpression.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var compilation = context.Document.Project.GetCompilationAsync(context.CancellationToken);
            var newDocument = await ComputeExpressionAsync(context.Document, diagnostic.Location, context.CancellationToken).ConfigureAwait(false);
            if (newDocument != null)
                context.RegisterCodeFix(CodeAction.Create("Compute expression", c => Task.FromResult(newDocument), nameof(ComputeExpressionCodeFixProvider)), diagnostic);
        }

        private async static Task<Document> ComputeExpressionAsync(Document document, Location diagnosticLocation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnosticLocation.SourceSpan);
            var parenthesized = node as ParenthesizedExpressionSyntax;
            var expression = (BinaryExpressionSyntax)(parenthesized != null ? parenthesized.Expression : node is ArgumentSyntax ? ((ArgumentSyntax)node).Expression : node);
            var newRoot = ComputeExpression(node, expression, root, semanticModel);
            if (newRoot == null) return null;
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        internal static SyntaxNode ComputeExpression(SyntaxNode nodeToReplace, BinaryExpressionSyntax expression, SyntaxNode root, SemanticModel semanticModel)
        {
            var result = semanticModel.GetConstantValue(expression);
            if (!result.HasValue) return null;
            SyntaxNode newExpression = SyntaxFactory.ParseExpression(System.Convert.ToString(result.Value, System.Globalization.CultureInfo.InvariantCulture));
            if(nodeToReplace is ArgumentSyntax)
            {
                newExpression = SyntaxFactory.Argument((ExpressionSyntax)newExpression);
            }
            var newRoot = root.ReplaceNode(nodeToReplace, newExpression);
            return newRoot;
        }
    }
}