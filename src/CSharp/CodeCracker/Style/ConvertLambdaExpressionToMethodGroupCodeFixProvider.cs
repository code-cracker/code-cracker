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

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertLambdaExpressionToMethodGroupCodeFixProvider)), Shared]
    public class ConvertLambdaExpressionToMethodGroupCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ConvertLambdaExpressionToMethodGroup.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(
                "Use method name instead of lambda expression when signatures match", c => ConvertAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async Task<Document> ConvertAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var lambda = root.FindToken(diagnosticSpan.Start).Parent
                .AncestorsAndSelf().First(x => x.Kind() == SyntaxKind.SimpleLambdaExpression ||
                                               x.Kind() == SyntaxKind.ParenthesizedLambdaExpression);
            var methodInvoke = ConvertLambdaExpressionToMethodGroupAnalyzer.GetInvocationIfAny(lambda);
            var newRoot = root.ReplaceNode(lambda as ExpressionSyntax, methodInvoke.Expression as ExpressionSyntax);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}