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

namespace CodeCracker.CSharp.Style
{

    [ExportCodeFixProvider("CodeCrackerUseStringEmptyCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UseStringEmptyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.UseStringEmpty.ToDiagnosticId());
        public readonly static string MessageFormat = "Use 'String.Empty' instead of \"\"";
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var localDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().First();
            const string message = "Use 'String.Empty'";
            context.RegisterCodeFix(CodeAction.Create(message, c => UseStringEmptyAsync(context.Document, localDeclaration, c)), diagnostic);
        }

        private async Task<Document> UseStringEmptyAsync(Document document, LiteralExpressionSyntax literalDeclaration, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(literalDeclaration,SyntaxFactory.ParseExpression("string.Empty"));
            var newDocument = document.WithSyntaxRoot(newRoot);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}