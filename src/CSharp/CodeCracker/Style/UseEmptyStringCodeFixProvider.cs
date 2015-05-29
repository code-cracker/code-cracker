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
    [ExportCodeFixProvider("CodeCrackerUseEmptyStringCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UseEmptyStringCodeFixProvider : CodeFixProvider
    {
        private const string EmptyString = "\"\"";
        public const string MessageFormat = "Use " + EmptyString;

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.UseEmptyString.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => UseEmptyStringCodeFixAllProvider.Instance;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(MessageFormat, c => UseEmptyStringAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async Task<Document> UseEmptyStringAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var literal = root.FindNode(diagnostic.Location.SourceSpan).DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().First();
            var newRoot = root.ReplaceNode(literal, SyntaxFactory.ParseExpression(EmptyString).WithLeadingTrivia(literal.GetLeadingTrivia()).WithTrailingTrivia(literal.GetTrailingTrivia()));
            var newDocument = document.WithSyntaxRoot(newRoot);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}