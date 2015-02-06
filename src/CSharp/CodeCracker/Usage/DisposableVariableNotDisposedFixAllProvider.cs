using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.Usage
{
    public sealed class DisposableVariableNotDisposedFixAllProvider : FixAllProvider
    {
        private static readonly SyntaxAnnotation disposeAnnotation = new SyntaxAnnotation("DisposableVariableNotDisposedFixAllProvider");
        private DisposableVariableNotDisposedFixAllProvider() { }
        public static DisposableVariableNotDisposedFixAllProvider Instance = new DisposableVariableNotDisposedFixAllProvider();
        public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return CodeAction.Create(DisposableVariableNotDisposedCodeFixProvider.MessageFormat,
                        fixAllContext.Document.WithSyntaxRoot(await GetFixedDocumentAsync(fixAllContext, fixAllContext.Document).ConfigureAwait(false)));
                case FixAllScope.Project:
                    return CodeAction.Create(DisposableVariableNotDisposedCodeFixProvider.MessageFormat,
                        await GetFixedProjectAsync(fixAllContext, fixAllContext.Project).ConfigureAwait(false));
                case FixAllScope.Solution:
                    return CodeAction.Create(DisposableVariableNotDisposedCodeFixProvider.MessageFormat,
                        await GetFixedSolutionAsync(fixAllContext).ConfigureAwait(false));
            }
            return null;
        }

        private async Task<Solution> GetFixedSolutionAsync(FixAllContext fixAllContext)
        {
            var newSolution = fixAllContext.Solution;
            foreach (var projectId in newSolution.ProjectIds)
                newSolution = await GetFixedProjectAsync(fixAllContext, newSolution.GetProject(projectId)).ConfigureAwait(false);
            return newSolution;
        }

        private async Task<Solution> GetFixedProjectAsync(FixAllContext fixAllContext, Project project)
        {
            var solution = project.Solution;
            var newDocuments = project.Documents.ToDictionary(d => d.Id, d => GetFixedDocumentAsync(fixAllContext, d));
            await Task.WhenAll(newDocuments.Values).ConfigureAwait(false);
            foreach (var newDoc in newDocuments)
                solution = solution.WithDocumentSyntaxRoot(newDoc.Key, newDoc.Value.Result);
            return solution;
        }

        private async Task<SyntaxNode> GetFixedDocumentAsync(FixAllContext fixAllContext, Document document)
        {
            var diagnostics = await fixAllContext.GetDiagnosticsAsync(document).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            var nodes = diagnostics.Select(d => root.FindNode(d.Location.SourceSpan)).Where(n => !n.IsMissing);
            var newRoot = root.ReplaceNodes(nodes, (original, rewritten) => original.WithAdditionalAnnotations(disposeAnnotation));
            var semanticModel = await document.GetSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            while (true)
            {
                var annotatedNodes = newRoot.GetAnnotatedNodes(disposeAnnotation);
                var node = annotatedNodes.FirstOrDefault();
                if (node == null) break;
                newRoot = DisposableVariableNotDisposedCodeFixProvider.CreateUsing(newRoot, (ObjectCreationExpressionSyntax)node, semanticModel);
                node = newRoot.GetAnnotatedNodes(disposeAnnotation).First();
                newRoot = newRoot.ReplaceNode(node, node.WithoutAnnotations(disposeAnnotation));
            }
            return newRoot;
        }
    }
}
