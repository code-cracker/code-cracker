using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertLambdaExpressionToMethodGroupFixProvider)), Shared]
    public class ConvertLambdaExpressionToMethodGroupFixProvider :CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ConvertLambdaExpressionToMethodGroup.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var lambda = root.FindToken(diagnosticSpan.Start).Parent
                .AncestorsAndSelf().First(x => x.Kind() == SyntaxKind.SimpleLambdaExpression ||
                                               x.Kind() == SyntaxKind.ParenthesizedLambdaExpression);
            var methodInvoke = ConvertLambdaExpressionToMethodGroupAnalyzer.GetInvocationIfAny(lambda);
            var newRoot = root.ReplaceNode(lambda as ExpressionSyntax, methodInvoke.Expression as ExpressionSyntax);
            var newDocument = context.Document.WithSyntaxRoot(newRoot);
            context.RegisterCodeFix(CodeAction.Create(
                "Use method name instead of lambda expression when signatures match", ct => Task.FromResult(newDocument)), diagnostic);
        }
    }
}