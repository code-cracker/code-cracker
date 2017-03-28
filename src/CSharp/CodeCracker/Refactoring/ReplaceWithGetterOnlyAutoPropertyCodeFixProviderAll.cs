using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Refactoring
{
    public interface IFixDocumentInternalsOnly
    {
        Task<Document> FixDocumentAsync(SyntaxNode nodeWithDiagnostic, Document document, CancellationToken cancellationToken);
    }

    public sealed class DocumentCodeFixProviderAll<CodeFixer> : FixAllProvider where CodeFixer : IFixDocumentInternalsOnly
    {
        private readonly static SyntaxAnnotation DiagnosticNodeSyntaxAnnotation = new SyntaxAnnotation("DocumentCodeFixProviderAllSyntaxAnnotation");

        public DocumentCodeFixProviderAll(CodeFixer codeFixer) {
            DocumentCodeFixer = codeFixer;
        }

        private CodeFixer DocumentCodeFixer { get; }

        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            switch (fixAllContext.Scope)
            {
                case FixAllScope.Document:
                    return Task.FromResult(CodeAction.Create(Resources.ReplaceWithGetterOnlyAutoPropertyCodeFixProvider_Title,
                        async ct => fixAllContext.Document.WithSyntaxRoot(await GetFixedDocumentAsync(fixAllContext, fixAllContext.Document))));
                case FixAllScope.Project:
                    return Task.FromResult(CodeAction.Create(Resources.ReplaceWithGetterOnlyAutoPropertyCodeFixProvider_Title,
                        ct => GetFixedProjectAsync(fixAllContext, fixAllContext.Project)));
                case FixAllScope.Solution:
                    return Task.FromResult(CodeAction.Create(Resources.ReplaceWithGetterOnlyAutoPropertyCodeFixProvider_Title,
                        ct => GetFixedSolutionAsync(fixAllContext)));
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
            var changedDocumentsSyntaxRoots = from kvp in newDocuments
                                              where kvp.Value.Result != null
                                              select new { DocumentId = kvp.Key, SyntaxRoot = kvp.Value.Result };
            foreach (var newDocumentSyntaxRoot in changedDocumentsSyntaxRoots)
                solution = solution.WithDocumentSyntaxRoot(newDocumentSyntaxRoot.DocumentId, newDocumentSyntaxRoot.SyntaxRoot);
            return solution;
        }

        private async Task<SyntaxNode> GetFixedDocumentAsync(FixAllContext fixAllContext, Document document)
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            if (diagnostics.Length == 0) return null;
            var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            var nodes = diagnostics.Select(d => root.FindNode(d.Location.SourceSpan)).Where(n => !n.IsMissing);
            var newRoot = root.ReplaceNodes(nodes, (original, rewritten) => original.WithAdditionalAnnotations(DiagnosticNodeSyntaxAnnotation));
            var newDocument = document.WithSyntaxRoot(newRoot);
            newRoot = await newDocument.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            while (true)
            {
                var annotatedNodes = newRoot.GetAnnotatedNodes(DiagnosticNodeSyntaxAnnotation);
                var node = annotatedNodes.FirstOrDefault();
                if (node == null) break;

                newDocument = await DocumentCodeFixer.FixDocumentAsync(node, newDocument, fixAllContext.CancellationToken);
                newRoot = await newDocument.GetSyntaxRootAsync();
                node = newRoot.GetAnnotatedNodes(DiagnosticNodeSyntaxAnnotation).First();
                newRoot = newRoot.ReplaceNode(node, node.WithoutAnnotations(DiagnosticNodeSyntaxAnnotation));
                newDocument = newDocument.WithSyntaxRoot(newRoot);
                newRoot = await newDocument.GetSyntaxRootAsync();
            }
            return newRoot;
        }
    }
}