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
            var returnIdentifier = (IdentifierNameSyntax)(getterReturn.Expression is MemberAccessExpressionSyntax
                ? ((MemberAccessExpressionSyntax)getterReturn.Expression).Name
                : getterReturn.Expression);
            var fieldSymbol = (IFieldSymbol)semanticModel.GetSymbolInfo(returnIdentifier).Symbol;
            var variableDeclarator = (VariableDeclaratorSyntax)fieldSymbol.DeclaringSyntaxReferences.First().GetSyntax();
            var fieldDeclaration = variableDeclarator.FirstAncestorOfType<FieldDeclarationSyntax>();
            var propertySymbol = semanticModel.GetDeclaredSymbol(property);
            var newRoot = root.TrackNodes(returnIdentifier, fieldDeclaration, property, variableDeclarator);
            //cycle
            var newDocument = document.WithSyntaxRoot(newRoot);
            newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken);
            var newProperty = CreateAutoProperty(property, variableDeclarator, fieldSymbol, propertySymbol);
            Solution newSolution;
            if (IsExplicityImplementation(propertySymbol))
            {
                semanticModel = await newDocument.GetSemanticModelAsync(cancellationToken);
                returnIdentifier = newRoot.GetCurrentNode(returnIdentifier);
                fieldSymbol = (IFieldSymbol)semanticModel.GetSymbolInfo(returnIdentifier).Symbol;
                newSolution = await RenameSymbolAndKeepExplicitPropertiesBoundAsync(newDocument.Project.Solution, property.Identifier.ValueText, fieldSymbol, propertySymbol, cancellationToken);
                newDocument = newSolution.GetDocument(newDocument.Id);
                newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                newRoot = newRoot.ReplaceNode(newRoot.GetCurrentNode(property), newProperty);
                newSolution = newSolution.WithDocumentSyntaxRoot(newDocument.Id, newRoot);
            }
            else
            {
                var currentProperty = newRoot.GetCurrentNode(property);
                var type = (TypeDeclarationSyntax)currentProperty.Parent;
                var propertyIndex = type.Members.IndexOf(currentProperty);
                //Remove the property: this is needed otherwise the rename that happens bellow will not be able to
                //correctly redirect the references to the field, as the property will conflict with the name.
                //The conflict is specially troublesome for circular references, such as the one that caused the bug #702.
                newRoot = newRoot.ReplaceNode(type, type.RemoveNode(currentProperty, SyntaxRemoveOptions.KeepNoTrivia));
                //cycle
                newDocument = newDocument.WithSyntaxRoot(newRoot);
                newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken);
                semanticModel = await newDocument.GetSemanticModelAsync(cancellationToken);
                fieldSymbol = (IFieldSymbol)semanticModel.GetDeclaredSymbol(newRoot.GetCurrentNode(variableDeclarator));
                //rename the field:
                newSolution = await Renamer.RenameSymbolAsync(newDocument.Project.Solution, fieldSymbol, property.Identifier.ValueText, newDocument.Project.Solution.Workspace.Options, cancellationToken).ConfigureAwait(false);
                //cycle
                newDocument = newSolution.GetDocument(newDocument.Id);
                newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                //add the property back:
                var currentType = (TypeDeclarationSyntax)newRoot.GetCurrentNode(fieldDeclaration).Parent;
                var newMembers = currentType.Members.Insert(propertyIndex, newProperty);
                var newType = WithMembers(currentType, newMembers);
                newRoot = newRoot.ReplaceNode(currentType, newType);
                newSolution = newSolution.WithDocumentSyntaxRoot(newDocument.Id, newRoot);
            }
            newDocument = newSolution.GetDocument(newDocument.Id);
            newRoot = await newDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            newRoot = RemoveField(newRoot, variableDeclarator, fieldDeclaration);
            return newSolution.WithDocumentSyntaxRoot(newDocument.Id, newRoot);
        }

        private static SyntaxNode RemoveField(SyntaxNode root, VariableDeclaratorSyntax variableDeclarator, FieldDeclarationSyntax fieldDeclaration)
        {
            var currentField = root.GetCurrentNode(fieldDeclaration);
            var multipleVariableDeclaration = fieldDeclaration.Declaration.Variables.Count > 1;
            root = multipleVariableDeclaration
                ? root.ReplaceNode(currentField, fieldDeclaration
                    .WithDeclaration(fieldDeclaration.Declaration.RemoveNode(variableDeclarator, SyntaxRemoveOptions.KeepNoTrivia)))
                : root.RemoveNode(currentField, SyntaxRemoveOptions.KeepNoTrivia);
            return root;
        }

        private static async Task<Solution> RenameSymbolAndKeepExplicitPropertiesBoundAsync(Solution solution, string propertyName,
            ISymbol returnIdentifierSymbol, IPropertySymbol propertySymbol, CancellationToken cancellationToken)
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

        private static PropertyDeclarationSyntax CreateAutoProperty(PropertyDeclarationSyntax property, VariableDeclaratorSyntax variableDeclarator,
            IFieldSymbol fieldSymbol, IPropertySymbol propertySymbol)
        {
            var newProperty = property.WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List(new[] {
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)),
                    SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                })));
            newProperty = variableDeclarator.Initializer == null ?
                newProperty :
                newProperty.WithInitializer(variableDeclarator.Initializer)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            newProperty = CreatePropertyWithCorrectAccessibility(newProperty, fieldSymbol, propertySymbol);
            newProperty = newProperty
                .WithTriviaFrom(property)
                .WithAdditionalAnnotations(Formatter.Annotation);
            return newProperty;
        }

        private static PropertyDeclarationSyntax CreatePropertyWithCorrectAccessibility(PropertyDeclarationSyntax property, IFieldSymbol fieldSymbol, IPropertySymbol propertySymbol)
        {
            if (IsExplicityImplementation(propertySymbol))
                return property;
            var existingModifiers = property.Modifiers.Where(m =>
            {
                var modifierText = m.ValueText;
                return modifierText != "private"
                    && modifierText != "protected"
                    && modifierText != "public"
                    && modifierText != "internal";
            });
            var newAccessibilityModifiers = propertySymbol.DeclaredAccessibility
                .GetMinimumCommonAccessibility(fieldSymbol.DeclaredAccessibility)
                .GetTokens()
                .Aggregate(existingModifiers, (ts, t) =>
                    ts.Any(tt => tt.ValueText == t.ValueText) ? ts : ts.Union(new[] { t }).ToArray())
                .OrderBy(t => t.ValueText);
            var newProperty = property.WithModifiers(SyntaxFactory.TokenList(newAccessibilityModifiers));
            return newProperty;
        }

        private static bool IsExplicityImplementation(IPropertySymbol propertySymbol) => propertySymbol.ExplicitInterfaceImplementations.Any();

        private static TypeDeclarationSyntax WithMembers(TypeDeclarationSyntax type, SyntaxList<MemberDeclarationSyntax> newMembers)
        {
            TypeDeclarationSyntax newType;
            var classDeclaration = type as ClassDeclarationSyntax;
            if (classDeclaration != null)
            {
                newType = classDeclaration.WithMembers(newMembers);
            }
            else
            {
                var structDeclaration = (StructDeclarationSyntax)type;
                newType = structDeclaration.WithMembers(newMembers);
            }
            return newType;
        }
    }
}