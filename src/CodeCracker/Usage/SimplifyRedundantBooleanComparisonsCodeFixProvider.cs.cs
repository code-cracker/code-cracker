using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeCracker.Usage
{
    [ExportCodeFixProvider("CodeCrackerArgumentExceptionCodeFixProvider", LanguageNames.CSharp), Shared]
    public class SimplifyRedundantBooleanComparisonsCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(SimplifyRedundantBooleanComparisonsAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var comparison = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BinaryExpressionSyntax>().First();

            context.RegisterFix(CodeAction.Create("Removes redundant comparision", c => RemoveRedundantComparisonAsync(context.Document, comparison, c)), diagnostic);
        }

        private async Task<Document> RemoveRedundantComparisonAsync(Document document, BinaryExpressionSyntax comparison, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var right = (bool) semanticModel.GetConstantValue(comparison.Right).Value;

            var replacer = comparison.Left;

            if ((!right && comparison.IsKind(SyntaxKind.EqualsExpression)) || (right && comparison.IsKind(SyntaxKind.NotEqualsExpression)))
            {
                replacer = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, replacer);
            }
            
            replacer = replacer.WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(comparison, replacer);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}
