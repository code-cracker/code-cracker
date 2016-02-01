using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SwitchToAutoPropCodeFixProvider)), Shared]
    public class SwitchToAutoPropCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.SwitchToAutoProp.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => SwitchToAutoPropCodeFixAllProvider.Instance;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(Resources.SwitchToAutoPropCodeFixProvider_Title, c => MakeAutoPropertyAsync(context.Document, diagnostic, c), nameof(SwitchToAutoPropCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Solution> MakeAutoPropertyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var property = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();
            return await MakeAutoPropertyAsync(document, root, property, cancellationToken);
        }

        public async static Task<Solution> MakeAutoPropertyAsync(Document document, SyntaxNode root, PropertyDeclarationSyntax property, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var getterReturn = (ReturnStatementSyntax)property.AccessorList.Accessors.First(a => a.Keyword.ValueText == "get").Body.Statements.First();
            var returnIdentifier = (IdentifierNameSyntax)(getterReturn.Expression is MemberAccessExpressionSyntax ? ((MemberAccessExpressionSyntax)getterReturn.Expression).Name : getterReturn.Expression);
            var returnIdentifierSymbol = semanticModel.GetSymbolInfo(returnIdentifier).Symbol;
            var variableDeclarator = (VariableDeclaratorSyntax)returnIdentifierSymbol.DeclaringSyntaxReferences.First().GetSyntax();
            var fieldDeclaration = variableDeclarator.FirstAncestorOfType<FieldDeclarationSyntax>();
            var fieldSymbol = (IFieldSymbol)semanticModel.GetDeclaredSymbol(variableDeclarator);
            var propertySymbol = semanticModel.GetDeclaredSymbol(property);
            root = root.TrackNodes(returnIdentifier, fieldDeclaration, property);
            document = document.WithSyntaxRoot(root);
            root = await document.GetSyntaxRootAsync(cancellationToken);
            semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            returnIdentifier = root.GetCurrentNode(returnIdentifier);
            returnIdentifierSymbol = semanticModel.GetSymbolInfo(returnIdentifier).Symbol;
            var newProperty = GetSimpleProperty(property, variableDeclarator);
            Solution newSolution;
            if (!propertySymbol.ExplicitInterfaceImplementations.Any())
            {
                newProperty = newProperty.WithModifiers(propertySymbol.DeclaredAccessibility
                    .GetMinimumCommonAccessibility(fieldSymbol.DeclaredAccessibility)
                    .GetTokens());
                newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, returnIdentifierSymbol, property.Identifier.ValueText, document.Project.Solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
            }
            else
            {
                newSolution = await RenameSymbolAndKeepExplicitPropertiesBoundAsync(document.Project.Solution, property.Identifier.ValueText, returnIdentifierSymbol, propertySymbol, cancellationToken);
            }
            newProperty = newProperty
                .WithTriviaFrom(property)
                .WithAdditionalAnnotations(Formatter.Annotation);
            document = newSolution.GetDocument(document.Id);
            root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            root = root.InsertNodesAfter(root.GetCurrentNode(property), new[] { newProperty });
            var multipleVariableDeclaration = fieldDeclaration.Declaration.Variables.Count > 1;
            if (multipleVariableDeclaration)
            {
                var newfieldDeclaration = fieldDeclaration.WithDeclaration(fieldDeclaration.Declaration.RemoveNode(variableDeclarator, SyntaxRemoveOptions.KeepNoTrivia));
                root = root.RemoveNode(root.GetCurrentNode<SyntaxNode>(property), SyntaxRemoveOptions.KeepNoTrivia);
                root = root.ReplaceNode(root.GetCurrentNode(fieldDeclaration), newfieldDeclaration);
            }
            else
            {
                root = root.RemoveNodes(root.GetCurrentNodes<SyntaxNode>(new SyntaxNode[] { fieldDeclaration, property }), SyntaxRemoveOptions.KeepNoTrivia);
            }
            document = document.WithSyntaxRoot(root);
            return document.Project.Solution;
        }

        private static async Task<Solution> RenameSymbolAndKeepExplicitPropertiesBoundAsync(Solution solution, string propertyName, ISymbol returnIdentifierSymbol, IPropertySymbol propertySymbol, CancellationToken cancellationToken)
        {
            var interfaceType = propertySymbol.ExplicitInterfaceImplementations.First().ContainingType;
            var references = await SymbolFinder.FindReferencesAsync(returnIdentifierSymbol, solution, cancellationToken).ConfigureAwait(false);
            var documentGroups = references.SelectMany(r => r.Locations).GroupBy(loc => loc.Document);
            var newSolution = solution;
            var propertyIdentifier = SyntaxFactory.IdentifierName(propertyName);
            foreach (var documentGroup in documentGroups)
            {
                var referencingDocument = documentGroup.Key;
                referencingDocument = newSolution.GetDocument(referencingDocument.Id);
                var referencingDocRoot = await referencingDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var diagnosticNodes = documentGroup.Select(referenceLocation => referencingDocRoot.FindNode(referenceLocation.Location.SourceSpan)).ToList();
                referencingDocRoot = referencingDocRoot.TrackNodes(diagnosticNodes);
                foreach (var diagnosticNode in diagnosticNodes)
                {
                    var trackedNode = referencingDocRoot.GetCurrentNode(diagnosticNode);
                    var identifierName = (IdentifierNameSyntax)trackedNode;
                    var memberAccess = identifierName.FirstAncestorOfKind<MemberAccessExpressionSyntax>(SyntaxKind.SimpleMemberAccessExpression);
                    if (memberAccess != null && memberAccess.Name == identifierName)
                    {
                        var newMemberAccess = memberAccess.WithExpression(
                            SyntaxFactory.ParenthesizedExpression(
                                SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(interfaceType.ToString()), memberAccess.Expression)))
                            .WithName(propertyIdentifier)
                            .WithAdditionalAnnotations(Simplifier.Annotation);
                        referencingDocRoot = referencingDocRoot.ReplaceNode(memberAccess, newMemberAccess);
                    }
                    else
                    {
                        var newMemberAccess = SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParenthesizedExpression(
                                SyntaxFactory.CastExpression(SyntaxFactory.ParseTypeName(interfaceType.ToString()), SyntaxFactory.ParseExpression("this"))),
                            propertyIdentifier)
                            .WithAdditionalAnnotations(Simplifier.Annotation);
                        referencingDocRoot = referencingDocRoot.ReplaceNode(trackedNode, newMemberAccess);
                    }
                }
                newSolution = newSolution.WithDocumentSyntaxRoot(referencingDocument.Id, referencingDocRoot);
            }
            return newSolution;
        }

        private static PropertyDeclarationSyntax GetSimpleProperty(PropertyDeclarationSyntax property, VariableDeclaratorSyntax variableDeclarator)
        {
            var simpleGetSetPropetie = property.WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                })));
            return variableDeclarator.Initializer == null ?
                simpleGetSetPropetie :
                simpleGetSetPropetie.WithInitializer(variableDeclarator.Initializer)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
        }
    }
}