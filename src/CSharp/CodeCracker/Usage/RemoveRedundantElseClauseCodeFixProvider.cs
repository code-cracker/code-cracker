using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider("CodeCrackerRemoveRedundantElseClauseCodeFixProvider", LanguageNames.CSharp), Shared]
    public class RemoveRedundantElseClauseCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.RemoveRedundantElseClause.ToDiagnosticId());

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var @else = root.FindToken(diagnosticSpan.Start).GetAncestor<ElseClauseSyntax>();
            root = root.RemoveNode(@else, SyntaxRemoveOptions.KeepNoTrivia);
            var newDocument = context.Document.WithSyntaxRoot(root);
            context.RegisterCodeFix(CodeAction.Create(diagnostic.GetMessage(), ct => Task.FromResult(newDocument)), diagnostic);
        }
    }
}
