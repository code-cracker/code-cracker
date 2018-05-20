using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InconsistentAccessibilityCodeFixProvider)), Shared]
    public sealed class InconsistentAccessibilityCodeFixProvider : CodeFixProvider
    {
        internal const string InconsistentAccessibilityInMethodReturnTypeCompilerErrorNumber = "CS0050";
        internal const string InconsistentAccessibilityInMethodParameterCompilerErrorNumber = "CS0051";
        internal const string InconsistentAccessibilityInFieldTypeCompilerErrorNumber = "CS0052";
        internal const string InconsistentAccessibilityInPropertyTypeCompilerErrorNumber = "CS0053";
        internal const string InconsistentAccessibilityInIndexerReturnTypeCompilerErrorNumber = "CS0054";
        internal const string InconsistentAccessibilityInIndexerParameterCompilerErrorNumber = "CS0055";

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(InconsistentAccessibilityInMethodReturnTypeCompilerErrorNumber, InconsistentAccessibilityInMethodParameterCompilerErrorNumber, InconsistentAccessibilityInFieldTypeCompilerErrorNumber, InconsistentAccessibilityInPropertyTypeCompilerErrorNumber, InconsistentAccessibilityInIndexerReturnTypeCompilerErrorNumber, InconsistentAccessibilityInIndexerParameterCompilerErrorNumber);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            foreach (var diagnostic in context.Diagnostics)
            {
                var inconsistentAccessibilityInfo = await GetInconsistentAccessibilityInfoAsync(context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);

                if (inconsistentAccessibilityInfo.TypeToChangeFound())
                {
                    var typeLocations = await FindTypeLocationsInSourceCodeAsync(context.Document, inconsistentAccessibilityInfo.TypeToChangeAccessibility, context.CancellationToken).ConfigureAwait(false);

                    if (typeLocations.Length == 1)
                    {
                        context.RegisterCodeFix(CodeAction.Create(inconsistentAccessibilityInfo.CodeActionMessage, c => ChangeTypeAccessibilityInDocumentAsync(context.Document.Project.Solution, inconsistentAccessibilityInfo.NewAccessibilityModifiers, typeLocations[0], c), nameof(InconsistentAccessibilityCodeFixProvider)), diagnostic);
                    }
                    else if (typeLocations.Length > 1)
                    {
                        context.RegisterCodeFix(CodeAction.Create(inconsistentAccessibilityInfo.CodeActionMessage, c => ChangeTypeAccessibilityInSolutionAsync(context.Document.Project.Solution, inconsistentAccessibilityInfo.NewAccessibilityModifiers, typeLocations, c), nameof(InconsistentAccessibilityCodeFixProvider)), diagnostic);
                    }
                }
            }
        }

        private static async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            InconsistentAccessibilityInfoProvider inconsistentAccessibilityProvider = null;

            switch (diagnostic.Id)
            {
                case InconsistentAccessibilityInMethodReturnTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInMethodReturnType();
                    break;
                case InconsistentAccessibilityInMethodParameterCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInMethodParameter();
                    break;
                case InconsistentAccessibilityInFieldTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInFieldType();
                    break;
                case InconsistentAccessibilityInPropertyTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInPropertyType();
                    break;
                case InconsistentAccessibilityInIndexerReturnTypeCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInIndexerReturnType();
                    break;
                case InconsistentAccessibilityInIndexerParameterCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInIndexerParameter();
                    break;
                default:
                    break;
            }

            return await inconsistentAccessibilityProvider.GetInconsistentAccessibilityInfoAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Location[]> FindTypeLocationsInSourceCodeAsync(Document document, TypeSyntax type, CancellationToken cancellationToken)
        {
            var result = new Location[] { };

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = semanticModel.GetSymbolInfo(type, cancellationToken).Symbol;

            if(typeSymbol != null)
            {
                result = typeSymbol.Locations.Where(location => location.IsInSource).ToArray();
            }

            return result;
        }

        private static async Task<Document> ChangeTypeAccessibilityInDocumentAsync(Solution solution, SyntaxTokenList newAccessibilityModifiers, Location typeLocation, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(typeLocation.SourceTree);
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var newRoot = ChangeTypeAccessibilityInSyntaxRoot(syntaxRoot, newAccessibilityModifiers, typeLocation);

            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<Solution> ChangeTypeAccessibilityInSolutionAsync(Solution solution, SyntaxTokenList newAccessibilityModifiers, IEnumerable<Location> typeLocations, CancellationToken cancellationToken)
        {
            var updatedSolution = solution;
            var typeLocationsGroupedByDocument = typeLocations.GroupBy(location => solution.GetDocument(location.SourceTree));

            foreach(var typeLocationsWithinDocument in typeLocationsGroupedByDocument)
            {
                var document = typeLocationsWithinDocument.Key;
                var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
                var newRoot = ChangeTypesAccessibilityInSyntaxRoot(syntaxRoot, newAccessibilityModifiers, typeLocationsWithinDocument);
                updatedSolution = updatedSolution.WithDocumentSyntaxRoot(document.Id, newRoot);
            }

            return updatedSolution;
        }

        private static SyntaxNode ChangeTypeAccessibilityInSyntaxRoot(SyntaxNode syntaxRoot, SyntaxTokenList newAccessibilityModifiers, Location typeLocation)
        {
            var declaration = (MemberDeclarationSyntax)syntaxRoot.FindNode(typeLocation.SourceSpan);

            var newDeclaration = ChangeAccessibilityModifiersInDeclaration(declaration, newAccessibilityModifiers);

            return syntaxRoot.ReplaceNode(declaration, newDeclaration);
        }

        private static SyntaxNode ChangeTypesAccessibilityInSyntaxRoot(SyntaxNode syntaxRoot, SyntaxTokenList newAccessibilityModifiers, IEnumerable<Location> typeLocations)
        {
            var declarations =  typeLocations.Select(typeLocation => (MemberDeclarationSyntax)syntaxRoot.FindNode(typeLocation.SourceSpan)).ToList();
            var newDeclarations = new Dictionary<MemberDeclarationSyntax, MemberDeclarationSyntax>();

            foreach (var declaration in declarations)
            {
                newDeclarations.Add(declaration, ChangeAccessibilityModifiersInDeclaration(declaration, newAccessibilityModifiers));
            }

            return syntaxRoot.ReplaceNodes(declarations, (original, rewritten) => newDeclarations[original]);
        }

        private static MemberDeclarationSyntax ChangeAccessibilityModifiersInDeclaration(MemberDeclarationSyntax declaration, SyntaxTokenList newAccessibilityModifiers)
        {
            var newDeclaration = declaration;

            var actualTypeAccessibilityModifiers = GetAccessibilityModifiersFromMember(declaration);
            var hasAccessibilityModifiers = actualTypeAccessibilityModifiers.Any();

            var leadingTrivias = default(SyntaxTriviaList);
            var trailingTrivias = default(SyntaxTriviaList);
            if (!hasAccessibilityModifiers)
            {
                var modifiers = declaration.GetModifiers();
                if (modifiers.Any())
                {
                    var firstModifier = modifiers.First();
                    leadingTrivias = firstModifier.LeadingTrivia;
                    newDeclaration = RemoveLeadingTriviasFromFirstDeclarationModifier(declaration, modifiers, firstModifier);
                }
                else
                {
                    leadingTrivias = declaration.GetLeadingTrivia();
                    newDeclaration = RemoveLeadingTriviasFromDeclaration(declaration);
                }
                trailingTrivias = SyntaxFactory.TriviaList(SyntaxFactory.Space);
            }
            else
            {
                leadingTrivias = actualTypeAccessibilityModifiers.First().LeadingTrivia;
                trailingTrivias = GetAllTriviasAfterFirstModifier(actualTypeAccessibilityModifiers);
            }

            newAccessibilityModifiers = MergeActualTriviasIntoNewAccessibilityModifiers(newAccessibilityModifiers, leadingTrivias, trailingTrivias);

            newDeclaration = ReplaceDeclarationModifiers(newDeclaration, actualTypeAccessibilityModifiers.ToList(), newAccessibilityModifiers);

            return newDeclaration;
        }

        private static IEnumerable<SyntaxToken> GetAccessibilityModifiersFromMember(MemberDeclarationSyntax member) => member.GetModifiers().Where(token => token.IsKind(SyntaxKind.PublicKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword, SyntaxKind.PrivateKeyword));

        private static MemberDeclarationSyntax RemoveLeadingTriviasFromFirstDeclarationModifier(MemberDeclarationSyntax declaration, SyntaxTokenList modifiers, SyntaxToken modifier) => declaration.WithModifiers(modifiers.Replace(modifier, modifier.WithLeadingTrivia(default(SyntaxTriviaList))));

        private static MemberDeclarationSyntax RemoveLeadingTriviasFromDeclaration(MemberDeclarationSyntax declaration) => declaration.WithoutLeadingTrivia();

        private static SyntaxTriviaList GetAllTriviasAfterFirstModifier(IEnumerable<SyntaxToken> modifiers) => SyntaxFactory.TriviaList(modifiers.Skip(1).SelectMany(token => token.LeadingTrivia).Union(modifiers.SelectMany(token => token.TrailingTrivia)));

        private static MemberDeclarationSyntax ReplaceDeclarationModifiers(MemberDeclarationSyntax declaration, List<SyntaxToken> oldAccessibilityModifiers, SyntaxTokenList newAccessibilityModifiers)
        {
            var result = declaration;
            var replacedModifiers = declaration.GetModifiers();

            if (oldAccessibilityModifiers.Count == 0)
            {
                replacedModifiers = replacedModifiers.InsertRange(0,newAccessibilityModifiers);
            }
            else if(oldAccessibilityModifiers.Count == 1)
            {
                replacedModifiers = replacedModifiers.ReplaceRange(oldAccessibilityModifiers[0], newAccessibilityModifiers);
            }
            else if(oldAccessibilityModifiers.Count == 2)
            {
                replacedModifiers = replacedModifiers.ReplaceRange(oldAccessibilityModifiers[0], newAccessibilityModifiers);
                replacedModifiers = replacedModifiers.Remove(replacedModifiers.SingleOrDefault(token => token.IsKind(oldAccessibilityModifiers[1].Kind())));
            }

            result = declaration.WithModifiers(replacedModifiers);

            return result;
        }

        private static SyntaxTokenList MergeActualTriviasIntoNewAccessibilityModifiers(SyntaxTokenList modifiers, SyntaxTriviaList leadingTrivias, SyntaxTriviaList trailingTrivias)
        {
            if (modifiers.Count == 1)
            {
                modifiers = modifiers.Replace(modifiers[0], modifiers[0].WithLeadingTrivia(leadingTrivias).WithTrailingTrivia(trailingTrivias));
            }
            else if (modifiers.Count == 2)
            {
                modifiers = modifiers.Replace(modifiers[0], modifiers[0].WithLeadingTrivia(leadingTrivias));
                modifiers = modifiers.Replace(modifiers[1], modifiers[1].WithTrailingTrivia(trailingTrivias));
            }

            return modifiers;
        }
    }
}
