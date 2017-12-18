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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TaskNameAsyncCodeFixProvider)), Shared]
    public class TaskNameAsyncCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.TaskNameAsync.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            if (GetMethodName(diagnostic) == "Main" && IsUsingCSharp7(diagnostic))
                return Task.FromResult(0);

            context.RegisterCodeFix(CodeAction.Create("Change method name including 'Async'.", c => ChangeMethodNameAsync(context.Document, diagnostic, c), nameof(TaskNameAsyncCodeFixProvider)), diagnostic);

            return Task.FromResult(0);
        }

        private static string GetMethodName(Diagnostic diagnostic)
        {
            return diagnostic.Location.SourceTree.ToString().Substring(diagnostic.Location.SourceSpan.Start, diagnostic.Location.SourceSpan.End - diagnostic.Location.SourceSpan.Start);
        }

        private static bool IsUsingCSharp7(Diagnostic diagnostic)
        {
            return ((CSharpParseOptions)diagnostic.Location.SourceTree.Options).LanguageVersion.ToString() == "CSharp7";
        }

        private static async Task<Solution> ChangeMethodNameAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var methodStatement = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
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