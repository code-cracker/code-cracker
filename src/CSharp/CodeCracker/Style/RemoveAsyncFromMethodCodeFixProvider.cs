using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveAsyncFromMethodCodeFixProvider)), Shared]
    public class RemoveAsyncFromMethodCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.RemoveAsyncFromMethod.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Remove Async termination for method name.",
                c => RemoveAsyncTerminationAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async Task<Solution> RemoveAsyncTerminationAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var method = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            var newName = method.Identifier.Text.Replace("Async", "");
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var symbol = semanticModel.GetDeclaredSymbol(method, cancellationToken);
            var solution = document.Project.Solution;
            var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, newName, solution.Workspace.Options, cancellationToken);
            return newSolution;
        }
    }
}