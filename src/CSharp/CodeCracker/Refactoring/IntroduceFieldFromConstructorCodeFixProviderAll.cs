using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Refactoring
{
    public sealed class IntroduceFieldFromConstructorCodeFixAllProvider : FixAllProvider
    {
        private static readonly SyntaxAnnotation introduceFieldAnnotation = new SyntaxAnnotation(nameof(IntroduceFieldFromConstructorCodeFixAllProvider));
        private IntroduceFieldFromConstructorCodeFixAllProvider() { }
        public static readonly IntroduceFieldFromConstructorCodeFixAllProvider Instance = new IntroduceFieldFromConstructorCodeFixAllProvider();

        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult(CodeAction.Create(IntroduceFieldFromConstructorCodeFixProvider.MessageFormat,
                        async ct => fixAllContext.Document.WithSyntaxRoot(await GetFixedDocumentAsync(fixAllContext, fixAllContext.Document))));
                case FixAllScope.Project:
                    return Task.FromResult(CodeAction.Create(IntroduceFieldFromConstructorCodeFixProvider.MessageFormat,
                        ct => GetFixedProjectAsync(fixAllContext, fixAllContext.Project)));
                case FixAllScope.Solution:
                    return Task.FromResult(CodeAction.Create(IntroduceFieldFromConstructorCodeFixProvider.MessageFormat,
                        ct => GetFixedSolutionAsync(fixAllContext)));
            }
            return null;
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
            var newRoot = root.ReplaceNodes(nodes, (original, rewritten) => original.WithAdditionalAnnotations(introduceFieldAnnotation));
            var semanticModel = await document.GetSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            while (true)
            {
                var annotatedNodes = newRoot.GetAnnotatedNodes(introduceFieldAnnotation);
                var node = annotatedNodes.FirstOrDefault();
                if (node == null) break;

                var constructorMethod = (ConstructorDeclarationSyntax)node.Parent.Parent;
                var parameter = (ParameterSyntax)node;
                newRoot = IntroduceFieldFromConstructorCodeFixProvider.IntroduceFieldFromConstructor(newRoot, constructorMethod, parameter);
                node = newRoot.GetAnnotatedNodes(introduceFieldAnnotation).First();
                newRoot = newRoot.ReplaceNode(node, node.WithoutAnnotations(introduceFieldAnnotation));
            }
            return newRoot;
        }
    }
}
