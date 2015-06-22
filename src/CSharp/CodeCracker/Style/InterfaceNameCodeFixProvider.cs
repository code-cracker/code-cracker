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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InterfaceNameCodeFixProvider)), Shared]
    public class InterfaceNameCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.InterfaceName.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Consider start Interface name with letter 'I'", c => ChangeInterfaceNameAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Solution> ChangeInterfaceNameAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var interfaceStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InterfaceDeclarationSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var newName = "I" + interfaceStatement.Identifier.ToString();
            var solution = document.Project.Solution;
            if (solution == null) return null;
            var symbol = semanticModel.GetDeclaredSymbol(interfaceStatement, cancellationToken);
            if (symbol == null) return null;
            var options = solution.Workspace.Options;
            var newSolution = await Renamer.RenameSymbolAsync(solution, symbol, newName,
                options, cancellationToken).ConfigureAwait(false);
            return newSolution;
        }
    }
}