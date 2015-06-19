using CodeCracker.Properties;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConsoleWriteLineCodeFixProvider)), Shared]
    public class ConsoleWriteLineCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ConsoleWriteLine.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(Resources.ConsoleWriteLineCodeFixProvider_Title, c => MakeStringInterpolationAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> MakeStringInterpolationAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var invocationExpression = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var newStringInterpolation = await StringFormatCodeFixProvider.CreateNewStringInterpolationAsync(document, root, invocationExpression, cancellationToken);
            var newArgumentList = SyntaxFactory.ArgumentList(SyntaxFactory.SeparatedList<ArgumentSyntax>().Add(SyntaxFactory.Argument(newStringInterpolation)));
            var newRoot = root.ReplaceNode(invocationExpression.ArgumentList, newArgumentList);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}