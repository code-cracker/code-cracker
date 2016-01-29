using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
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
            var references = await SymbolFinder.FindReferencesAsync(methodSymbol, document.Project.Solution, cancellationToken).ConfigureAwait(false);
            var documentGroups = references.SelectMany(r => r.Locations).GroupBy(loc => loc.Document);
            var fullMethodName = methodSymbol.GetFullName();
            var newSolution = await UpdateMainDocumentAsync(document, fullMethodName, root, method, documentGroups, cancellationToken);
            newSolution = await UpdateReferencingDocumentsAsync(document, fullMethodName, documentGroups, newSolution, cancellationToken).ConfigureAwait(false);
            return newSolution;
        }

        private static async Task<Solution> UpdateMainDocumentAsync(Document document, string fullMethodName, SyntaxNode root, MethodDeclarationSyntax method, IEnumerable<IGrouping<Document, ReferenceLocation>> documentGroups, CancellationToken cancellationToken)
        {
            var mainDocGroup = documentGroups.FirstOrDefault(dg => dg.Key.Equals(document));
            SyntaxNode newRoot;
            if (mainDocGroup == null)
            {
                newRoot = root.ReplaceNode(method, method.WithoutTrivia().AddModifiers(staticToken).WithTriviaFrom(method));
            }
            else
            {
                var newMemberAccess = (MemberAccessExpressionSyntax)SyntaxFactory.ParseExpression(fullMethodName);
                newMemberAccess = newMemberAccess.WithExpression(
                    newMemberAccess.Expression.WithAdditionalAnnotations(Simplifier.Annotation));
                var diagnosticNodes = mainDocGroup.Select(referenceLocation => root.FindNode(referenceLocation.Location.SourceSpan)).ToList();
                newRoot = root.TrackNodes(diagnosticNodes.Union(new[] { method }));
                var trackedMethod = newRoot.GetCurrentNode(method);
                var staticMethod = method.WithoutTrivia().AddModifiers(staticToken).WithTriviaFrom(method);
                newRoot = newRoot.ReplaceNode(trackedMethod, staticMethod);
                foreach (var diagnosticNode in diagnosticNodes)
                {
                    var tempDoc = document.WithSyntaxRoot(newRoot);
                    newRoot = await tempDoc.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                    var semanticModel = await tempDoc.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
                    var syntaxNode = newRoot.GetCurrentNode(diagnosticNode);
                    var memberAccess = syntaxNode.FirstAncestorOrSelfOfType<MemberAccessExpressionSyntax>();
                    if (memberAccess?.Expression == null) continue;
                    if (!syntaxNode.Equals(memberAccess.Name)) continue;
                    var memberAccessExpressionSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
                    var containingMember = memberAccess.FirstAncestorOrSelfThatIsAMember();
                    var memberSymbol = semanticModel.GetDeclaredSymbol(containingMember);
                    var allContainingTypes = memberSymbol.GetAllContainingTypes().ToList();
                    SyntaxNode expressionToReplaceMemberAccess;
                    var methodTypeSymbol = GetMethodTypeSymbol(memberAccessExpressionSymbol);
                    if (allContainingTypes.Any(t => t.Equals(methodTypeSymbol)))
                    {
                        // ideally we would check the symbols
                        // but there is a bug on Roslyn 1.0, fixed on 1.1:
                        // https://github.com/dotnet/roslyn/issues/3096
                        // so if we try to check the method symbol, it fails and always returns null
                        // so if we find a name clash, whatever one, we fall back to the full name
                        expressionToReplaceMemberAccess = allContainingTypes.Count(t => t.MemberNames.Any(n => n == memberSymbol.Name)) > 1
                            ? newMemberAccess
                            : (SyntaxNode)memberAccess.Name;
                    }
                    else
                    {
                        expressionToReplaceMemberAccess = newMemberAccess;
                    }
                    var newMemberAccessParent = memberAccess.Parent.ReplaceNode(memberAccess, expressionToReplaceMemberAccess)
                        .WithAdditionalAnnotations(Formatter.Annotation)
                        .WithAdditionalAnnotations(Simplifier.Annotation);
                    newRoot = newRoot.ReplaceNode(memberAccess.Parent, newMemberAccessParent);
                }
            }
            var newSolution = document.Project.Solution.WithDocumentSyntaxRoot(document.Id, newRoot);
            return newSolution;
        }

        private static ISymbol GetMethodTypeSymbol(ISymbol memberAccessExpressionSymbol)
        {
            ISymbol methodTypeSymbol = null;
            var methodSymbol = memberAccessExpressionSymbol as IMethodSymbol;
            if (methodSymbol != null)
                methodTypeSymbol = methodSymbol.MethodKind == MethodKind.Constructor ? methodSymbol.ReceiverType : methodSymbol.ReturnType;
            var parameterSymbol = memberAccessExpressionSymbol as IParameterSymbol;
            if (parameterSymbol != null)
                methodTypeSymbol = parameterSymbol.Type;
            return methodTypeSymbol;
        }

        private static async Task<Solution> UpdateReferencingDocumentsAsync(Document document, string fullMethodName, IEnumerable<IGrouping<Document, ReferenceLocation>> documentGroups, Solution newSolution, CancellationToken cancellationToken)
        {
            var newMemberAccess = (MemberAccessExpressionSyntax)SyntaxFactory.ParseExpression(fullMethodName);
            newMemberAccess = newMemberAccess.WithExpression(
                newMemberAccess.Expression.WithAdditionalAnnotations(Simplifier.Annotation));
            foreach (var documentGroup in documentGroups)
            {
                var referencingDocument = documentGroup.Key;
                if (referencingDocument.Id.Equals(document.Id)) continue;
                referencingDocument = newSolution.GetDocument(referencingDocument.Id);
                var root = await referencingDocument.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var diagnosticNodes = documentGroup.Select(referenceLocation => root.FindNode(referenceLocation.Location.SourceSpan)).ToList();
                root = root.TrackNodes(diagnosticNodes);
                foreach (var diagnosticNode in diagnosticNodes)
                {
                    var trackedNode = root.GetCurrentNode(diagnosticNode);
                    var memberAccess = trackedNode.FirstAncestorOrSelfOfType<MemberAccessExpressionSyntax>();
                    if (memberAccess?.Expression == null) continue;
                    if (!trackedNode.Equals(memberAccess.Name)) continue;
                    var newMemberAccessParent = memberAccess.Parent.ReplaceNode(memberAccess, newMemberAccess)
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    root = root.ReplaceNode(memberAccess.Parent, newMemberAccessParent);
                }
                newSolution = newSolution.WithDocumentSyntaxRoot(referencingDocument.Id, root);
            }
            return newSolution;
        }
    }
}