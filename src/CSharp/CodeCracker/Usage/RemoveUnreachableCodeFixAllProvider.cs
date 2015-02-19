using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Usage
{
    public sealed class RemoveUnreachableCodeFixAllProvider : FixAllProvider
    {
        private static readonly SyntaxAnnotation removeUnreachableCodeAnnotation = new SyntaxAnnotation("RemoveUnreachableCodeFixAllProvider");
        private RemoveUnreachableCodeFixAllProvider() { }
        public static RemoveUnreachableCodeFixAllProvider Instance = new RemoveUnreachableCodeFixAllProvider();
        public override async Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return CodeAction.Create(RemoveUnreachableCodeCodeFixProvider.Message,
                        fixAllContext.Document.WithSyntaxRoot(await GetFixedDocumentAsync(fixAllContext, fixAllContext.Document).ConfigureAwait(false)));
                case FixAllScope.Project:
                    return CodeAction.Create(RemoveUnreachableCodeCodeFixProvider.Message,
                        await GetFixedProjectAsync(fixAllContext, fixAllContext.Project).ConfigureAwait(false));
                case FixAllScope.Solution:
                    return CodeAction.Create(RemoveUnreachableCodeCodeFixProvider.Message,
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
            var nodes = diagnostics.Select(d => root.FindNode(d.Location.SourceSpan)).Where(n => !n.IsMissing).ToList();
            var newRoot = root;
            while (nodes.Any())
            {
                newRoot = newRoot.ReplaceNodes(nodes, (original, rewritten) => original.WithAdditionalAnnotations(removeUnreachableCodeAnnotation));
                while (true)
                {
                    var annotatedNodes = newRoot.GetAnnotatedNodes(removeUnreachableCodeAnnotation);
                    var node = annotatedNodes.FirstOrDefault();
                    if (node == null) break;
                    newRoot = RemoveUnreachableCodeCodeFixProvider.RemoveUnreachableStatement(newRoot, node);
                }
                var newDoc = document.WithSyntaxRoot(newRoot);
                diagnostics = await fixAllContext.GetDiagnosticsAsync(newDoc).ConfigureAwait(false);
                newRoot = await newDoc.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                nodes = diagnostics.Select(d => newRoot.FindNode(d.Location.SourceSpan)).Where(n => !n.IsMissing).ToList();
            }
            return newRoot;
        }
    }
}