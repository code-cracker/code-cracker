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
        private static readonly SyntaxAnnotation removeUnreachableCodeAnnotation = new SyntaxAnnotation(nameof(RemoveUnreachableCodeFixAllProvider));
        private RemoveUnreachableCodeFixAllProvider() { }
        public static readonly RemoveUnreachableCodeFixAllProvider Instance = new RemoveUnreachableCodeFixAllProvider();
        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult(CodeAction.Create(RemoveUnreachableCodeCodeFixProvider.Message,
                        async ct => fixAllContext.Document.WithSyntaxRoot(await GetFixedDocumentAsync(fixAllContext, fixAllContext.Document).ConfigureAwait(false))));
                case FixAllScope.Project:
                    return Task.FromResult(CodeAction.Create(RemoveUnreachableCodeCodeFixProvider.Message,
                        ct => GetFixedProjectAsync(fixAllContext, fixAllContext.Project)));
                case FixAllScope.Solution:
                    return Task.FromResult(CodeAction.Create(RemoveUnreachableCodeCodeFixProvider.Message,
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
                diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(newDoc).ConfigureAwait(false);
                newRoot = await newDoc.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                nodes = diagnostics.Select(d => newRoot.FindNode(d.Location.SourceSpan)).Where(n => !n.IsMissing).ToList();
            }
            return newRoot;
        }
    }
}