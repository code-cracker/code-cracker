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
            var newSolution = UpdateMainDocument(document, root, methodClassName, method, documentGroups);
            newSolution = await UpdateReferencingDocumentsAsync(document, methodClassName, documentGroups, newSolution, cancellationToken);
            return newSolution;
        }

        private static Solution UpdateMainDocument(Document document, SyntaxNode root, string methodClassName, MethodDeclarationSyntax method, IEnumerable<IGrouping<Document, ReferenceLocation>> documentGroups)
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
                    newRoot = CreateNewRoot(newRoot, methodClassName, diagnosticNode);
                }
            }
            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            return newSolution;
        }

        private static SyntaxNode CreateNewRoot(SyntaxNode root, string methodClassName , SyntaxNode diagnosticNode)
        {
            var unchangedRoot = root;
            var memberAccess = root.GetCurrentNode(diagnosticNode).FirstAncestorOrSelfOfType<MemberAccessExpressionSyntax>();
            if (memberAccess == null) return unchangedRoot;
            if (IsTypeOfExpresion<ThisExpressionSyntax>(memberAccess.Expression))
            {
                var newMemberAccessParent = memberAccess.Parent.ReplaceNode(memberAccess, memberAccess.Name)
                    .WithAdditionalAnnotations(Formatter.Annotation);
                return root.ReplaceNode(memberAccess.Parent, newMemberAccessParent);
            }
            else if (IsTypeOfExpresion<ObjectCreationExpressionSyntax>(memberAccess.Expression))
            {
                var newObjectCreationExpression = SyntaxFactory.ExpressionStatement(GetObjectCreationExpresion(memberAccess.Expression));
                var nodeToInsertBefor = GetLastNodeBeforeBlockSyntax(root.GetCurrentNode(diagnosticNode));
                var oldBlock = nodeToInsertBefor.Parent;
                var newBlock = oldBlock.InsertNodesBefore(nodeToInsertBefor, new[] { newObjectCreationExpression });
                memberAccess = newBlock.GetCurrentNode(diagnosticNode).FirstAncestorOrSelfOfType<MemberAccessExpressionSyntax>();

                newBlock = newBlock.ReplaceNode(memberAccess.Expression, SyntaxFactory.IdentifierName(methodClassName))
                    .WithAdditionalAnnotations(Formatter.Annotation);
                return root.ReplaceNode(oldBlock, newBlock);
            }
            else
            {
                var newMemberAccessParent = memberAccess.Parent.ReplaceNode(memberAccess.Expression, SyntaxFactory.IdentifierName(methodClassName))
                        .WithAdditionalAnnotations(Formatter.Annotation);
                return root.ReplaceNode(memberAccess.Parent, newMemberAccessParent);
            }

        }

        private static SyntaxNode GetLastNodeBeforeBlockSyntax(SyntaxNode node)
        {
            while(node != null && node.Parent != null)
            {
                if (node.Parent.IsKind(SyntaxKind.Block)) return node;
                node = node.Parent;
            }
            return null;
        }

        private static bool IsTypeOfExpresion<T>(ExpressionSyntax expression) where T : SyntaxNode
        {
            while (expression != null)
            {
                if (expression.GetType() == typeof(T)) return true;
                expression = GetNextExpresion(expression);
            }
            return false;
        }

        private static ObjectCreationExpressionSyntax GetObjectCreationExpresion(ExpressionSyntax expression)
        {

            while (expression != null)
            {
                if (expression.IsKind(SyntaxKind.ObjectCreationExpression)) return expression as ObjectCreationExpressionSyntax;
                expression = GetNextExpresion(expression);
            }
            return null;
        }

        private static ExpressionSyntax GetNextExpresion(ExpressionSyntax expression)
        {
            if (expression.IsKind(SyntaxKind.ParenthesizedExpression))
                return (expression as ParenthesizedExpressionSyntax).Expression;
            else if (expression.IsKind(SyntaxKind.CastExpression))
                return (expression as CastExpressionSyntax).Expression;
            else
                return null;

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
                    newReferencingRoot = CreateNewRoot(newReferencingRoot, methodClassName, diagnosticNode);
                }
                newSolution = newSolution.WithDocumentSyntaxRoot(referencingDocument.Id, newReferencingRoot);
            }
            return newSolution;
        }
    }
}