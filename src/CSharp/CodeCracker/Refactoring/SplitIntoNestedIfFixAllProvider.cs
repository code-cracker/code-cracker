using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Refactoring
{
    public sealed class SplitIntoNestedIfFixAllProvider : FixAllProvider
    {
        private static readonly SyntaxAnnotation nestedIfAnnotation = new SyntaxAnnotation(nameof(SplitIntoNestedIfFixAllProvider));
        private SplitIntoNestedIfFixAllProvider() { }
        public static readonly SplitIntoNestedIfFixAllProvider Instance = new SplitIntoNestedIfFixAllProvider();
        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult(CodeAction.Create(SplitIntoNestedIfCodeFixProvider.MessageFormat,
                        async ct => fixAllContext.Document.WithSyntaxRoot(await GetFixedDocumentAsync(fixAllContext, fixAllContext.Document).ConfigureAwait(false))));
                case FixAllScope.Project:
                    return Task.FromResult(CodeAction.Create(SplitIntoNestedIfCodeFixProvider.MessageFormat,
                        ct => GetFixedProjectAsync(fixAllContext, fixAllContext.Project)));
                case FixAllScope.Solution:
                    return Task.FromResult(CodeAction.Create(SplitIntoNestedIfCodeFixProvider.MessageFormat,
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
            var newRoot = root.ReplaceNodes(nodes, (original, rewritten) => original.WithAdditionalAnnotations(nestedIfAnnotation));
            while (true)
            {
                var annotatedNodes = newRoot.GetAnnotatedNodes(nestedIfAnnotation);
                var condition = (BinaryExpressionSyntax)annotatedNodes.FirstOrDefault();
                if (condition == null) break;
                var ifStatement = condition.FirstAncestorOfType<IfStatementSyntax>();
                newRoot = SplitIntoNestedIfCodeFixProvider.CreateNestedIf(condition, newRoot);
            }
            return newRoot;
        }
    }
}