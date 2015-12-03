using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SimplifyRedundantBooleanComparisonsCodeFixProvider)), Shared]
    public class SimplifyRedundantBooleanComparisonsCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.SimplifyRedundantBooleanComparisons.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(
                "Removes redundant comparision", c => RemoveRedundantComparisonAsync(context.Document, diagnostic, c), nameof(SimplifyRedundantBooleanComparisonsCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> RemoveRedundantComparisonAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var comparison = root.FindToken(diagnostic.Location.SourceSpan.Start)
                .Parent.AncestorsAndSelf()
                .OfType<BinaryExpressionSyntax>()
                .First(bes => !bes.IsKind(SyntaxKind.IsExpression));

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


            if ((!constValue && comparison.IsKind(SyntaxKind.EqualsExpression)) ||
                (constValue && comparison.IsKind(SyntaxKind.NotEqualsExpression)))
            {
                if (comparison.Left is BinaryExpressionSyntax)
                {
                    replacer = SyntaxFactory.ParenthesizedExpression(replacer);
                }
                replacer = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, replacer);
            }

            replacer = replacer
                .WithAdditionalAnnotations(Formatter.Annotation);


            var newRoot = root.ReplaceNode(comparison, replacer);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}