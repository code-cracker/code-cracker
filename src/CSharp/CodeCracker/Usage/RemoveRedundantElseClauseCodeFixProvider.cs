using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveRedundantElseClauseCodeFixProvider)), Shared]
    public class RemoveRedundantElseClauseCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.RemoveRedundantElseClause.ToDiagnosticId());

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(RemoveRedundantElseClauseAnalyzer.MessageFormat, c => RemoveElseAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }
        private async static Task<Document> RemoveElseAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var @else = root.FindToken(diagnosticSpan.Start).GetAncestor<ElseClauseSyntax>();
            root = root.RemoveNode(@else, SyntaxRemoveOptions.KeepNoTrivia);
            var newDocument = document.WithSyntaxRoot(root);
            return newDocument;
        }
    }
}