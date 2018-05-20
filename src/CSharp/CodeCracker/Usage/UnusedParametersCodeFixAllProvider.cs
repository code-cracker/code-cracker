using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Usage
{
    public sealed class UnusedParametersCodeFixAllProvider : FixAllProvider
    {
        private UnusedParametersCodeFixAllProvider() { }

        private const string message = "Remove unused parameter";
        public static readonly UnusedParametersCodeFixAllProvider Instance = new UnusedParametersCodeFixAllProvider();
        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult(CodeAction.Create(message,
                        async ct => await GetFixedSolutionAsync(fixAllContext, await GetSolutionWithDocsAsync(fixAllContext, fixAllContext.Document))));
                case FixAllScope.Project:
                    return Task.FromResult(CodeAction.Create(message,
                        async ct => await GetFixedSolutionAsync(fixAllContext, await GetSolutionWithDocsAsync(fixAllContext, fixAllContext.Project))));
                case FixAllScope.Solution:
                    return Task.FromResult(CodeAction.Create(message,
                        async ct => await GetFixedSolutionAsync(fixAllContext, await GetSolutionWithDocsAsync(fixAllContext, fixAllContext.Solution))));
                default:
                    return null;
            }
        }

        private async static Task<SolutionWithDocs> GetSolutionWithDocsAsync(FixAllContext fixAllContext, Solution solution)
        {
            var docs = new List<DiagnosticsInDoc>();
            var sol = new SolutionWithDocs { Docs = docs, Solution = solution };
            foreach (var pId in solution.Projects.Select(p => p.Id))
            {
                var project = sol.Solution.GetProject(pId);
                var newSol = await GetSolutionWithDocsAsync(fixAllContext, project).ConfigureAwait(false);
                sol.Merge(newSol);
            }
            return sol;
        }

        private async static Task<SolutionWithDocs> GetSolutionWithDocsAsync(FixAllContext fixAllContext, Project project)
        {
            var docs = new List<DiagnosticsInDoc>();
            var newSolution = project.Solution;
            foreach (var document in project.Documents)
            {
                var doc = await GetDiagnosticsInDocAsync(fixAllContext, document);
                if (doc.Equals(DiagnosticsInDoc.Empty)) continue;
                docs.Add(doc);
                newSolution = newSolution.WithDocumentSyntaxRoot(document.Id, doc.TrackedRoot);
            }
            var sol = new SolutionWithDocs { Docs = docs, Solution = newSolution };
            return sol;
        }

        private async static Task<SolutionWithDocs> GetSolutionWithDocsAsync(FixAllContext fixAllContext, Document document)
        {
            var docs = new List<DiagnosticsInDoc>();
            var doc = await GetDiagnosticsInDocAsync(fixAllContext, document);
            docs.Add(doc);
            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, doc.TrackedRoot);
            var sol = new SolutionWithDocs { Docs = docs, Solution = newSolution };
            return sol;
        }

        private static async Task<DiagnosticsInDoc> GetDiagnosticsInDocAsync(FixAllContext fixAllContext, Document document)
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            if (!diagnostics.Any()) return DiagnosticsInDoc.Empty;
            var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            var doc = DiagnosticsInDoc.Create(document.Id, diagnostics, root);
            return doc;
        }

        private async static Task<Solution> GetFixedSolutionAsync(FixAllContext fixAllContext, SolutionWithDocs sol)
        {
            var newSolution = sol.Solution;
            foreach (var doc in sol.Docs)
            {
                foreach (var node in doc.Nodes)
                {
                    var document = newSolution.GetDocument(doc.DocumentId);
                    var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
                    var trackedNode = root.GetCurrentNode(node);
                    var parameter = trackedNode.AncestorsAndSelf().OfType<ParameterSyntax>().First();
                    var docResults = await UnusedParametersCodeFixProvider.RemoveParameterAsync(document, parameter, root, fixAllContext.CancellationToken);
                    foreach (var docResult in docResults)
                        newSolution = newSolution.WithDocumentSyntaxRoot(docResult.DocumentId, docResult.Root);
                }
            }
            return newSolution;
        }

        private struct DiagnosticsInDoc
        {
            public static DiagnosticsInDoc Create(DocumentId documentId, IList<Diagnostic> diagnostics, SyntaxNode root)
            {
                var nodes = diagnostics.Select(d => root.FindNode(d.Location.SourceSpan)).Where(n => !n.IsMissing).ToList();
                var diagnosticsInDoc = new DiagnosticsInDoc
                {
                    DocumentId = documentId,
                    TrackedRoot = root.TrackNodes(nodes),
                    Nodes = nodes
                };
                return diagnosticsInDoc;
            }
            public DocumentId DocumentId;
            public List<SyntaxNode> Nodes;
            public SyntaxNode TrackedRoot;

            private static readonly DiagnosticsInDoc empty = new DiagnosticsInDoc();
            public static DiagnosticsInDoc Empty => empty;
        }

        private struct SolutionWithDocs
        {
            public Solution Solution;
            public List<DiagnosticsInDoc> Docs;
            public void Merge(SolutionWithDocs sol)
            {
                Solution = sol.Solution;
                Docs.AddRange(sol.Docs);
            }
        }
    }
}