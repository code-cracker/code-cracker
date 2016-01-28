using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.CSharp.Usage;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Options;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBracesToSwitchSectionsCodeFixProvider)), Shared]
    public class ConvertMethodToPropertyCodeFixProvider : CodeFixProvider
    {
        private static readonly SyntaxToken semicolonToken = SyntaxFactory.Token(SyntaxKind.SemicolonToken);
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.ConvertMethodToProperty.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {


            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(Resources.ConvertMethodToProperty_Title, ct => MakeMethodPropertyAsync(context.Document, diagnostic, ct), nameof(ConvertMethodToPropertyCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Solution> MakeMethodPropertyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var method = (MethodDeclarationSyntax)root.FindNode(diagnosticSpan);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            var foundDocument = false;
            var references = await SymbolFinder.FindReferencesAsync(methodSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
            var documentGroups = references.SelectMany(r => r.Locations).GroupBy(loc => loc.Document);
            var docs = new List<UnusedParametersCodeFixProvider.DocumentIdAndRoot>();
            var replacement = PropertyToUse(method);
            foreach (var documentGroup in documentGroups)
            {
                var referencingDocument = documentGroup.Key;
                SyntaxNode locRoot;
                var replacingArgs = new Dictionary<SyntaxNode, SyntaxNode>();
                if (referencingDocument.Equals(document))
                {
                    locRoot = root;
                    replacingArgs.Add(method, replacement);
                    foundDocument = true;
                }
                else
                {
                    var locSemanticModel = await referencingDocument.GetSemanticModelAsync(cancellationToken);
                    locRoot = await locSemanticModel.SyntaxTree.GetRootAsync(cancellationToken);
                }
                foreach (var loc in documentGroup)
                {
                    var methodIdentifier = locRoot.FindNode(loc.Location.SourceSpan);
                    InvocationExpressionSyntax invocation = null;//methodIdentifier.Parent as InvocationExpressionSyntax ?? methodIdentifier.Parent.Parent as InvocationExpressionSyntax;
                    var parent = methodIdentifier.Parent;
                    if (parent is InvocationExpressionSyntax)
                    {
                        invocation = (InvocationExpressionSyntax)parent;
                    }
                    else
                    {
                        if (parent is MemberAccessExpressionSyntax)
                        {
                            invocation = parent.Parent as InvocationExpressionSyntax;
                        }
                    }
                    if (invocation != null)
                    {
                        replacingArgs.Add(invocation, invocation.Expression.WithSameTriviaAs(invocation));
                    }
                    else
                    {
                        var toReplace = (CSharpSyntaxNode)(methodIdentifier.Parent is MemberAccessExpressionSyntax
                            ? methodIdentifier.Parent
                            : methodIdentifier);

                        var lamda = SyntaxFactory.ParenthesizedLambdaExpression(toReplace);
                        var isArgument = toReplace is ArgumentSyntax;
                        var replacementNode = isArgument
                            ? (SyntaxNode)((ArgumentSyntax)toReplace).WithExpression(lamda)
                            : lamda;

                        var argumentOrParentArgument = toReplace as ArgumentSyntax ?? toReplace.Parent as ArgumentSyntax;

                        var callingIdentifier  = ((InvocationExpressionSyntax)argumentOrParentArgument?.Parent?.Parent)?.Expression as
                            IdentifierNameSyntax;

                        if (callingIdentifier?.Identifier.Text != "nameof")
                        {
                            replacingArgs.Add(toReplace, replacementNode);
                        }


                    }

                }
                var newLocRoot = locRoot.ReplaceNodes(replacingArgs.Keys, (original, rewritten) => replacingArgs[original]);
                docs.Add(new UnusedParametersCodeFixProvider.DocumentIdAndRoot { DocumentId = referencingDocument.Id, Root = newLocRoot });
            }
            if (!foundDocument)
            {
                var newRoot = root.ReplaceNode(method, replacement);
                docs.Add(new UnusedParametersCodeFixProvider.DocumentIdAndRoot { DocumentId = document.Id, Root = newRoot });
            }

            var newSolution = document.Project.Solution;
            foreach (var doc in docs)
            {
                newSolution = newSolution.WithDocumentSyntaxRoot(doc.DocumentId, doc.Root);
            }
            return newSolution;
        }

        private static PropertyDeclarationSyntax PropertyToUse(MethodDeclarationSyntax method)
        {
            var propertyWithoutBody =
                SyntaxFactory.PropertyDeclaration(method.ReturnType, method.Identifier)
                    .WithModifiers(method.Modifiers)
                    .WithAdditionalAnnotations(Formatter.Annotation);
            PropertyDeclarationSyntax property;
            if (method.ExpressionBody == null)
            {
                var accessorDeclarationSyntax = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, method.Body);
                if (method.Body == null)
                {
                    accessorDeclarationSyntax = accessorDeclarationSyntax.WithSemicolonToken(semicolonToken);
                }
                property = propertyWithoutBody.
                    WithAccessorList(
                        SyntaxFactory.AccessorList(new SyntaxList<AccessorDeclarationSyntax>().Add
                            (
                                accessorDeclarationSyntax
                            )
                            )
                    );
            }
            else
            {
                property = propertyWithoutBody.WithExpressionBody(method.ExpressionBody).WithSemicolonToken(semicolonToken);
            }
            return property;
        }
    }
}
