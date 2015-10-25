using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
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

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

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
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);


            var getterReturn = (ReturnStatementSyntax)property.AccessorList.Accessors.First(a => a.Keyword.ValueText == "get").Body.Statements.First();
            var returnIdentifier = (IdentifierNameSyntax)getterReturn.Expression;
            var returnIdentifierSymbol = semanticModel.GetSymbolInfo(returnIdentifier).Symbol;


            var variableDeclarator = (VariableDeclaratorSyntax)returnIdentifierSymbol.DeclaringSyntaxReferences.First().GetSyntax();
            var fieldDeclaration = variableDeclarator.FirstAncestorOfType<FieldDeclarationSyntax>();


            root = root.TrackNodes(returnIdentifier, fieldDeclaration, property);
            document = document.WithSyntaxRoot(root);
            root = await document.GetSyntaxRootAsync(cancellationToken);
            semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            returnIdentifier = root.GetCurrentNode(returnIdentifier);
            returnIdentifierSymbol = semanticModel.GetSymbolInfo(returnIdentifier).Symbol;

            var newProperty = GetSimpleProperty(property, variableDeclarator)
                .WithTriviaFrom(property)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newSolution = await Renamer.RenameSymbolAsync(document.Project.Solution, returnIdentifierSymbol, property.Identifier.ValueText, document.Project.Solution.Workspace.Options, cancellationToken);
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