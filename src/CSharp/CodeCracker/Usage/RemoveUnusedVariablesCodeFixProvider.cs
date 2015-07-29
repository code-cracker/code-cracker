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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveUnusedVariablesCodeFixProvider)), Shared]
    public class RemoveUnusedVariablesCodeFixProvider : CodeFixProvider
    {

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create("CS0168", "CS0219");

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var parent = root.FindToken(diagnosticSpan.Start).Parent;

            if (!parent.IsKind(Microsoft.CodeAnalysis.CSharp.SyntaxKind.CatchDeclaration))
            {
                var variableUnused = parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();
                context.RegisterCodeFix(CodeAction.Create($"Remove unused variable : '{ variableUnused.Declaration.Variables.First()}'", c => RemoveVariableAsync(context.Document, variableUnused, c), nameof(RemoveUnusedVariablesCodeFixProvider)), diagnostic);
            }
            else
            {
                var variableUnused = parent.AncestorsAndSelf().OfType<CatchDeclarationSyntax>().First();
                context.RegisterCodeFix(CodeAction.Create($"Remove unused variable : '{ variableUnused.Identifier }'", c => RemoveVariableAsync(context.Document, variableUnused, c), nameof(RemoveUnusedVariablesCodeFixProvider)), diagnostic);
            }
        }

        private async static Task<Document> RemoveVariableAsync(Document document, LocalDeclarationStatementSyntax variableUnused, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = root.RemoveNode(variableUnused, SyntaxRemoveOptions.KeepNoTrivia);

            return document.WithSyntaxRoot(newRoot);
        }

        private async static Task<Document> RemoveVariableAsync(Document document, CatchDeclarationSyntax variableUnused, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = root.ReplaceNode(variableUnused, variableUnused.Update(variableUnused.OpenParenToken, variableUnused.Type.WithoutTrailingTrivia(), new SyntaxToken(), variableUnused.CloseParenToken));

            return document.WithSyntaxRoot(newRoot);
        }
    }
}