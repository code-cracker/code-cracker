using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System;
using CodeCracker.FixAllProviders;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReplaceWithGetterOnlyAutoPropertyCodeFixProvider)), Shared]
    public class ReplaceWithGetterOnlyAutoPropertyCodeFixProvider : CodeFixProvider, IFixDocumentInternalsOnly
    {
        private static readonly FixAllProvider FixAllProvider = new DocumentCodeFixProviderAll(Resources.ReplaceWithGetterOnlyAutoPropertyCodeFixProvider_Title);
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.ReplaceWithGetterOnlyAutoProperty.ToDiagnosticId());

        public override FixAllProvider GetFixAllProvider() => FixAllProvider;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: Resources.ReplaceWithGetterOnlyAutoPropertyCodeFixProvider_Title,
                    createChangedDocument: c => ReplaceByGetterOnlyAutoPropertyAsync(context.Document, diagnosticSpan, c),
                    equivalenceKey: nameof(ReplaceWithGetterOnlyAutoPropertyCodeFixProvider)),
                    diagnostic);
            return Task.FromResult(0);
        }
        private async Task<Document> ReplaceByGetterOnlyAutoPropertyAsync(Document document, TextSpan propertyDeclarationSpan, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(propertyDeclarationSpan);
            return await FixDocumentAsync(node, document, cancellationToken).ConfigureAwait(false);
        }

        public async Task<Document> FixDocumentAsync(SyntaxNode nodeWithDiagnostic, Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync().ConfigureAwait(false);
            var newRoot = await ReplacePropertyInSyntaxRootAsync(nodeWithDiagnostic, cancellationToken, semanticModel, root).ConfigureAwait(false);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }


        private static async Task<SyntaxNode> ReplacePropertyInSyntaxRootAsync(SyntaxNode propertyDeclarationSyntaxNode, CancellationToken cancellationToken, SemanticModel semanticModel, SyntaxNode root)
        {
            var property = propertyDeclarationSyntaxNode.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();
            var fieldVariableDeclaratorSyntax = await GetFieldDeclarationSyntaxNodeAsync(property, cancellationToken, semanticModel).ConfigureAwait(false);
            if (fieldVariableDeclaratorSyntax == null) return root;
            var fieldReferences = await GetFieldReferencesAsync(fieldVariableDeclaratorSyntax, cancellationToken, semanticModel).ConfigureAwait(false);
            var nodesToUpdate = fieldReferences.Cast<SyntaxNode>().Union(Enumerable.Repeat(property, 1)).Union(Enumerable.Repeat(fieldVariableDeclaratorSyntax, 1));
            var newRoot = FixWithTrackNode(root, property, fieldVariableDeclaratorSyntax, nodesToUpdate);
            return newRoot;
        }

        private static SyntaxNode FixWithTrackNode(SyntaxNode root, PropertyDeclarationSyntax property, VariableDeclaratorSyntax fieldVariableDeclaratorSyntax, IEnumerable<SyntaxNode> nodesToUpdate)
        {
            var newRoot = root.TrackNodes(nodesToUpdate);
            var fieldReferences = nodesToUpdate.OfType<IdentifierNameSyntax>();
            foreach (var identifier in fieldReferences)
            {
                var trackedIdentifierNode = newRoot.GetCurrentNode(identifier);
                var newIdentifierExpression = SyntaxFactory.IdentifierName(property.Identifier.Text);
                newIdentifierExpression = newIdentifierExpression.WithLeadingTrivia(trackedIdentifierNode.GetLeadingTrivia()).WithTrailingTrivia(trackedIdentifierNode.GetTrailingTrivia()).WithAdditionalAnnotations(Formatter.Annotation);
                newRoot = newRoot.ReplaceNode(trackedIdentifierNode, newIdentifierExpression);
            }
            var prop = newRoot.GetCurrentNode(nodesToUpdate.OfType<PropertyDeclarationSyntax>().Single());
            var fieldInitilization = GetFieldInitialization(fieldVariableDeclaratorSyntax);
            var getter = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            var accessorList = SyntaxFactory.AccessorList(
                SyntaxFactory.List(new[] {
                            getter
                }));
            var newProp = prop.WithAccessorList(accessorList);
            if (fieldInitilization != null)
                newProp = newProp.WithInitializer(fieldInitilization).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            newProp = newProp.WithLeadingTrivia(prop.GetLeadingTrivia()).WithTrailingTrivia(prop.GetTrailingTrivia()).WithAdditionalAnnotations(Formatter.Annotation);
            newRoot = newRoot.ReplaceNode(prop, newProp);
            var variableDeclarator = newRoot.GetCurrentNode(nodesToUpdate.OfType<VariableDeclaratorSyntax>().Single());
            var declaration = variableDeclarator.AncestorsAndSelf().OfType<VariableDeclarationSyntax>().First();
            if (declaration.Variables.Count == 1)
            {
                var fieldDeclaration = declaration.AncestorsAndSelf().OfType<FieldDeclarationSyntax>().First();
                newRoot = newRoot.RemoveNode(fieldDeclaration, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            }
            else
                newRoot = newRoot.RemoveNode(variableDeclarator, SyntaxRemoveOptions.KeepUnbalancedDirectives);
            return newRoot;
        }

        private static EqualsValueClauseSyntax GetFieldInitialization(VariableDeclaratorSyntax fieldVariableDeclaratorSyntax)
        {
            var declaration = fieldVariableDeclaratorSyntax.AncestorsAndSelf().OfType<VariableDeclarationSyntax>().First();
            if (declaration == null)
                return null;
            var variableWithPotentialInitializer = declaration.Variables.SkipWhile(v => v != fieldVariableDeclaratorSyntax).FirstOrDefault(v => v.Initializer != null);
            if (variableWithPotentialInitializer == null)
                return null;
            var initializer = variableWithPotentialInitializer.Initializer;
            return initializer;
        }

        private static async Task<IEnumerable<IdentifierNameSyntax>> GetFieldReferencesAsync(VariableDeclaratorSyntax fieldDeclarationSyntax, CancellationToken cancellationToken, SemanticModel semanticModel)
        {
            HashSet<IdentifierNameSyntax> fieldReferences = null;
            var fieldSymbol = semanticModel.GetDeclaredSymbol(fieldDeclarationSyntax, cancellationToken);
            var declaredInType = fieldSymbol.ContainingType;
            var references = declaredInType.DeclaringSyntaxReferences.Where(r => r.SyntaxTree == semanticModel.SyntaxTree);
            foreach (var reference in references)
            {
                var allNodes = (await reference.GetSyntaxAsync(cancellationToken)).DescendantNodes();
                var allFieldReferenceNodes = from n in allNodes.OfType<IdentifierNameSyntax>()
                                             where n.Identifier.Text == fieldSymbol.Name
                                             let nodeSymbolInfo = semanticModel.GetSymbolInfo(n, cancellationToken)
                                             where object.Equals(nodeSymbolInfo.Symbol, fieldSymbol)
                                             select n;
                foreach (var fieldReference in allFieldReferenceNodes)
                {
                    (fieldReferences ?? (fieldReferences = new HashSet<IdentifierNameSyntax>())).Add(fieldReference);
                }
            }
            return fieldReferences ?? Enumerable.Empty<IdentifierNameSyntax>();
        }

        private static async Task<VariableDeclaratorSyntax> GetFieldDeclarationSyntaxNodeAsync(PropertyDeclarationSyntax propertyDeclaration, CancellationToken cancellationToken, SemanticModel semanticModel)
        {
            var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration, cancellationToken);
            var declaredProperty = propertySymbol.GetMethod.DeclaringSyntaxReferences.FirstOrDefault();
            var declaredPropertySyntax = await declaredProperty.GetSyntaxAsync(cancellationToken);
            var fieldIdentifier = declaredPropertySyntax.DescendantNodesAndTokens().FirstOrDefault(n => n.IsNode && n.Kind() == SyntaxKind.IdentifierName);
            var fieldInfo = semanticModel.GetSymbolInfo(fieldIdentifier.AsNode());
            var fieldDeclaration = fieldInfo.Symbol.DeclaringSyntaxReferences.FirstOrDefault();
            var fieldDeclarationSyntax = await fieldDeclaration.GetSyntaxAsync();
            return fieldDeclarationSyntax as VariableDeclaratorSyntax;
        }
    }
}
