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
    [ExportCodeFixProvider("CodeCrackerArgumentExceptionCodeFixProvider", LanguageNames.CSharp), Shared]
    public class SimplifyRedundantBooleanComparisonsCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.SimplifyRedundantBooleanComparisons.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var comparison = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BinaryExpressionSyntax>().First();

            context.RegisterFix(CodeAction.Create("Removes redundant comparision", c => RemoveRedundantComparisonAsync(context.Document, comparison, c)), diagnostic);
        }

        private static async Task<Document> RemoveRedundantComparisonAsync(Document document, BinaryExpressionSyntax comparison, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            bool constValue;
            ExpressionSyntax replacer;
            var rightConst = semanticModel.GetConstantValue(comparison.Right);
            if (rightConst.HasValue)
            {
                constValue = (bool)rightConst.Value;
                replacer = comparison.Left;
            }
            else
            {
                var leftConst = semanticModel.GetConstantValue(comparison.Left);
                constValue = (bool)leftConst.Value;
                replacer = comparison.Right;
            }


            if ((!constValue && comparison.IsKind(SyntaxKind.EqualsExpression)) || (constValue && comparison.IsKind(SyntaxKind.NotEqualsExpression)))
                replacer = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, replacer);
            replacer = replacer.WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(comparison, replacer);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}