using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    public sealed class StringFormatFixAllProvider : FixAllProvider
    {
        private static readonly SyntaxAnnotation stringFormatAnnotation = new SyntaxAnnotation(nameof(StringFormatFixAllProvider));
        private StringFormatFixAllProvider() { }
        public static readonly StringFormatFixAllProvider Instance = new StringFormatFixAllProvider();
        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult(CodeAction.Create(Resources.StringFormatCodeFixProvider_Title,
                        async ct => fixAllContext.Document.WithSyntaxRoot(await GetFixedDocumentAsync(fixAllContext, fixAllContext.Document).ConfigureAwait(false))));
                case FixAllScope.Project:
                    return Task.FromResult(CodeAction.Create(Resources.StringFormatCodeFixProvider_Title,
                        ct => GetFixedProjectAsync(fixAllContext, fixAllContext.Project)));
                case FixAllScope.Solution:
                    return Task.FromResult(CodeAction.Create(Resources.StringFormatCodeFixProvider_Title,
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
            var nodes = diagnostics.Select(d => root.FindNode(d.Location.SourceSpan, getInnermostNodeForTie: true).FirstAncestorOrSelfOfType<InvocationExpressionSyntax>()).Where(n => !n.IsMissing).ToList();
            var newRoot = root.ReplaceNodes(nodes, (original, rewritten) => rewritten.WithAdditionalAnnotations(stringFormatAnnotation));
            while (true)
            {
                var annotatedNodes = newRoot.GetAnnotatedNodes(stringFormatAnnotation);
                var node = annotatedNodes.FirstOrDefault();
                if (node == null) break;
                newRoot = StringFormatCodeFixProvider.CreateNewStringInterpolation(newRoot, (InvocationExpressionSyntax)node);
            }
            return newRoot;
        }
    }
}