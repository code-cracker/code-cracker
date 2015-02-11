using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider("CodeCrackerTaskNameAsyncCodeFixProvider", LanguageNames.CSharp), Shared]
    public class TaskNameAsyncCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() => ImmutableArray.Create(DiagnosticId.TaskNameAsync.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            context.RegisterFix(CodeAction.Create("Change method name including 'Async'.", c => ChangeMethodNameAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Solution> ChangeMethodNameAsync(Document document, MethodDeclarationSyntax methodStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var newName = methodStatement.Identifier.ToString() + "Async";
            var solution = document.Project.Solution;
            var symbol = semanticModel.GetDeclaredSymbol(methodStatement, cancellationToken);
            var options = solution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, newName,
                options, cancellationToken).ConfigureAwait(false);
            return newSolution;
        }
    }
}