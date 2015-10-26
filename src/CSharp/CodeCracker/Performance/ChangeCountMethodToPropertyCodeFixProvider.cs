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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ChangeCountMethodToPropertyCodeFixProvider)), Shared]
    public class ChangeCountMethodToPropertyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ChangeCountMethodToProperty.ToDiagnosticId());
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;
        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var message = ChangeCountMethodToPropertyAnalyzer.MessageFormat.ToString();
            context.RegisterCodeFix(CodeAction.Create(message, c => ChangeCountMethodToPropertyAsync(context.Document, diagnostic, c), nameof(ChangeCountMethodToPropertyAnalyzer)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> ChangeCountMethodToPropertyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var memberExpression = invocation.Expression as MemberAccessExpressionSyntax;

            var newInvoke = invocation.WithExpression(memberExpression.WithName((SimpleNameSyntax)SyntaxFactory.ParseName("Count"))).Expression;
            var newRoot = root.ReplaceNode(invocation, newInvoke);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}