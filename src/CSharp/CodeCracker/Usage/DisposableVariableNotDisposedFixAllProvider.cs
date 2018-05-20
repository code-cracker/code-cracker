using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Usage
{
    public sealed class DisposableVariableNotDisposedFixAllProvider : FixAllProvider
    {
        private static readonly SyntaxAnnotation disposeAnnotation = new SyntaxAnnotation(nameof(DisposableVariableNotDisposedFixAllProvider));
        private DisposableVariableNotDisposedFixAllProvider() { }
        public static readonly DisposableVariableNotDisposedFixAllProvider Instance = new DisposableVariableNotDisposedFixAllProvider();
        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult(CodeAction.Create(DisposableVariableNotDisposedCodeFixProvider.MessageFormat,
                        async ct => fixAllContext.Document.WithSyntaxRoot(await GetFixedDocumentAsync(fixAllContext, fixAllContext.Document).ConfigureAwait(false))));
                case FixAllScope.Project:
                    return Task.FromResult(CodeAction.Create(DisposableVariableNotDisposedCodeFixProvider.MessageFormat,
                        ct => GetFixedProjectAsync(fixAllContext, fixAllContext.Project)));
                case FixAllScope.Solution:
                    return Task.FromResult(CodeAction.Create(DisposableVariableNotDisposedCodeFixProvider.MessageFormat,
                        ct => GetFixedSolutionAsync(fixAllContext)));
                default:
                    return null;
            }
        }

        private async static Task<Solution> GetFixedSolutionAsync(FixAllContext fixAllContext)
        {
            var newSolution = fixAllContext.Solution;
            foreach (var projectId in newSolution.ProjectIds)
                newSolution = await GetFixedProjectAsync(fixAllContext, newSolution.GetProject(projectId)).ConfigureAwait(false);
            return newSolution;
        }

        private async static Task<Solution> GetFixedProjectAsync(FixAllContext fixAllContext, Project project)
        {
            var solution = project.Solution;
            var newDocuments = project.Documents.ToDictionary(d => d.Id, d => GetFixedDocumentAsync(fixAllContext, d));
            await Task.WhenAll(newDocuments.Values).ConfigureAwait(false);
            foreach (var newDoc in newDocuments)
                solution = solution.WithDocumentSyntaxRoot(newDoc.Key, newDoc.Value.Result);
            return solution;
        }

        private async static Task<SyntaxNode> GetFixedDocumentAsync(FixAllContext fixAllContext, Document document)
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
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
