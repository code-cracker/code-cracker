using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UnusedParametersCodeFixProvider)), Shared]
    public class UnusedParametersCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.UnusedParameters.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => UnusedParametersCodeFixAllProvider.Instance;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(
                $"Remove unused parameter: '{diagnostic.Properties["identifier"]}'", c => RemoveParameterAsync(context.Document, diagnostic, c), nameof(UnusedParametersCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        public async static Task<Solution> RemoveParameterAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var newSolution = solution;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var parameter = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<ParameterSyntax>().First();
            var docs = await RemoveParameterAsync(document, parameter, root, cancellationToken);
            foreach (var doc in docs)
                newSolution = newSolution.WithDocumentSyntaxRoot(doc.DocumentId, doc.Root);
            return newSolution;
        }

        public async static Task<List<DocumentIdAndRoot>> RemoveParameterAsync(Document document, ParameterSyntax parameter, SyntaxNode root, CancellationToken cancellationToken)
        {
            var solution = document.Project.Solution;
            var parameterList = (ParameterListSyntax)parameter.Parent;
            var parameterPosition = parameterList.Parameters.IndexOf(parameter);
            var newParameterList = parameterList.WithParameters(parameterList.Parameters.Remove(parameter));
            var foundDocument = false;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var method = (BaseMethodDeclarationSyntax)parameter.Parent.Parent;
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            var references = await SymbolFinder.FindReferencesAsync(methodSymbol, solution, cancellationToken).ConfigureAwait(false);
            var documentGroups = references.SelectMany(r => r.Locations).GroupBy(loc => loc.Document);
            var docs = new List<DocumentIdAndRoot>();
            foreach (var documentGroup in documentGroups)
            {
                var referencingDocument = documentGroup.Key;
                SyntaxNode locRoot;
                SemanticModel locSemanticModel;
                var replacingArgs = new Dictionary<SyntaxNode, SyntaxNode>();
                if (referencingDocument.Equals(document))
                {
                    locSemanticModel = semanticModel;
                    locRoot = root;
                    replacingArgs.Add(parameterList, newParameterList);
                    foundDocument = true;
                }
                else
                {
                    locSemanticModel = await referencingDocument.GetSemanticModelAsync(cancellationToken);
                    locRoot = await locSemanticModel.SyntaxTree.GetRootAsync(cancellationToken);
                }
                foreach (var loc in documentGroup)
                {
                    var methodIdentifier = locRoot.FindNode(loc.Location.SourceSpan);
                    var objectCreation = methodIdentifier.Parent as ObjectCreationExpressionSyntax;
                    var arguments = objectCreation != null
                        ? objectCreation.ArgumentList
                        : methodIdentifier.FirstAncestorOfType<InvocationExpressionSyntax>().ArgumentList;
                    if (parameter.Modifiers.Any(m => m.IsKind(SyntaxKind.ParamsKeyword)))
                    {
                        var newArguments = arguments;
                        while (newArguments.Arguments.Count > parameterPosition)
                        {
                            newArguments = newArguments.WithArguments(newArguments.Arguments.RemoveAt(parameterPosition));
                        }
                        replacingArgs.Add(arguments, newArguments);
                    }
                    else
                    {
                        var newArguments = arguments.WithArguments(arguments.Arguments.RemoveAt(parameterPosition));
                        replacingArgs.Add(arguments, newArguments);
                    }
                }
                var newLocRoot = locRoot.ReplaceNodes(replacingArgs.Keys, (original, rewritten) => replacingArgs[original]);
                docs.Add(new DocumentIdAndRoot { DocumentId = referencingDocument.Id, Root = newLocRoot });
            }
            if (!foundDocument)
            {
                var newRoot = root.ReplaceNode(parameterList, newParameterList);
                var newDocument = document.WithSyntaxRoot(newRoot);
                docs.Add(new DocumentIdAndRoot { DocumentId = document.Id, Root = newRoot });
            }
            return docs;
        }
        public struct DocumentIdAndRoot
        {
            internal DocumentId DocumentId;
            internal SyntaxNode Root;
        }
    }
}