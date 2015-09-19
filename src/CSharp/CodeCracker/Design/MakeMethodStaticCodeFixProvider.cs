using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MakeMethodStaticCodeFixProvider)), Shared]
    public class MakeMethodStaticCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.MakeMethodStatic.ToDiagnosticId());

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Make static", c => MakeMethodStaticAsync(context.Document, diagnostic, c), nameof(MakeMethodStaticCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static readonly SyntaxToken staticToken = SyntaxFactory.Token(SyntaxKind.StaticKeyword);

        private static async Task<Solution> MakeMethodStaticAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var method = (MethodDeclarationSyntax)root.FindNode(diagnosticSpan);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            var methodClassName = methodSymbol.ContainingType.Name;
            var references = await SymbolFinder.FindReferencesAsync(methodSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
            var documentGroups = references.SelectMany(r => r.Locations).GroupBy(loc => loc.Document);
            var newSolution = UpdateMainDocument(document, root, method, documentGroups);
            newSolution = await UpdateReferencingDocumentsAsync(document, methodClassName, documentGroups, newSolution, cancellationToken);
            return newSolution;
        }

        private static Solution UpdateMainDocument(Document document, SyntaxNode root, MethodDeclarationSyntax method, IEnumerable<IGrouping<Document, ReferenceLocation>> documentGroups)
        {
            var mainDocGroup = documentGroups.FirstOrDefault(dg => dg.Key.Equals(document));
            SyntaxNode newRoot;
            if (mainDocGroup == null)
            {
                newRoot = root.ReplaceNode(method, method.WithoutTrivia().AddModifiers(staticToken).WithTriviaFrom(method));
            }
            else
            {
                var diagnosticNodes = mainDocGroup.Select(referenceLocation => root.FindNode(referenceLocation.Location.SourceSpan)).ToList();
                newRoot = root.TrackNodes(diagnosticNodes.Union(new[] { method }));
                newRoot = newRoot.ReplaceNode(newRoot.GetCurrentNode(method), method.WithoutTrivia().AddModifiers(staticToken).WithTriviaFrom(method));
                foreach (var diagnosticNode in diagnosticNodes)
                {
                    var token = newRoot.FindToken(diagnosticNode.GetLocation().SourceSpan.Start);
                    var tokenParent = token.Parent;
                    if (token.Parent.IsKind(SyntaxKind.IdentifierName)) continue;
                    var invocationExpression = newRoot.GetCurrentNode(diagnosticNode).FirstAncestorOrSelfOfType<InvocationExpressionSyntax>()?.Expression;
                    if (invocationExpression == null || invocationExpression.IsKind(SyntaxKind.IdentifierName)) continue;
                    var memberAccess = invocationExpression as MemberAccessExpressionSyntax;
                    if (memberAccess == null) continue;
                    var newMemberAccessParent = memberAccess.Parent.ReplaceNode(memberAccess, memberAccess.Name)
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    newRoot = newRoot.ReplaceNode(memberAccess.Parent, newMemberAccessParent);
                }
            }
            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            return newSolution;
        }

        private static async Task<Solution> UpdateReferencingDocumentsAsync(Document document, string methodClassName, IEnumerable<IGrouping<Document, ReferenceLocation>> documentGroups, Solution newSolution, CancellationToken cancellationToken)
        {
            var methodIdentifier = SyntaxFactory.IdentifierName(methodClassName);
            foreach (var documentGroup in documentGroups)
            {
                var referencingDocument = documentGroup.Key;
                if (referencingDocument.Equals(document)) continue;
                var newReferencingRoot = await referencingDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var diagnosticNodes = documentGroup.Select(referenceLocation => newReferencingRoot.FindNode(referenceLocation.Location.SourceSpan)).ToList();
                newReferencingRoot = newReferencingRoot.TrackNodes(diagnosticNodes);
                foreach (var diagnosticNode in diagnosticNodes)
                {
                    var memberAccess = (MemberAccessExpressionSyntax)newReferencingRoot.GetCurrentNode(diagnosticNode).FirstAncestorOrSelfOfType<InvocationExpressionSyntax>().Expression;
                    var newMemberAccess = memberAccess.ReplaceNode(memberAccess.Expression, methodIdentifier)
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    newReferencingRoot = newReferencingRoot.ReplaceNode(memberAccess, newMemberAccess);
                }
                newSolution = newSolution.WithDocumentSyntaxRoot(referencingDocument.Id, newReferencingRoot);
            }
            return newSolution;
        }
    }
}