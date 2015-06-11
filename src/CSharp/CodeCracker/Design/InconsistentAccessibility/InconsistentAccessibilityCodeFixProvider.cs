using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;
using System;
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
        internal const string InconsistentAccessibilityInMethodParameterCompilerErrorNumber = "CS0051";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(InconsistentAccessibilityInMethodParameterCompilerErrorNumber);

        public override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();

            var inconsistentAccessibilityInfo = await GetInconsistentAccessibilityInfoAsync(context.Document, diagnostic, context.CancellationToken).ConfigureAwait(false);

            var typeLocation = await FindTypeLocationInSourceCodeAsync(context.Document, inconsistentAccessibilityInfo.TypeToChangeAccessibility, context.CancellationToken).ConfigureAwait(false);

            if(typeLocation != Location.None)
            {
                context.RegisterCodeFix(CodeAction.Create(inconsistentAccessibilityInfo.CodeActionMessage, c => ChangeTypeAccessibilityAsync(context.Document.Project.Solution, inconsistentAccessibilityInfo.NewAccessibilityModifiers, typeLocation, c)), diagnostic);
            }
        }

        private static async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            InconsistentAccessibilityInfoProvider inconsistentAccessibilityProvider = null;

            switch (diagnostic.Id)
            {
                case InconsistentAccessibilityInMethodParameterCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInMethodParameter();
                    break;
            }

            return await inconsistentAccessibilityProvider.GetInconsistentAccessibilityInfoAsync(document, diagnostic, cancellationToken).ConfigureAwait(false);
        }

        private static async Task<Location> FindTypeLocationInSourceCodeAsync(Document document, TypeSyntax type, CancellationToken cancellationToken)
        {
            var result = Location.None;

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = semanticModel.GetSymbolInfo(type, cancellationToken).Symbol;

            if(typeSymbol != null)
            {
                var locationInSource = typeSymbol.Locations.FirstOrDefault(location => location.IsInSource);
                if(locationInSource != null)
                {
                    result = locationInSource;
                }
            }

            return result;
        }

        private static async Task<Document> ChangeTypeAccessibilityAsync(Solution solution, SyntaxTokenList newAccessibilityModifiers, Location typeLocation, CancellationToken cancellationToken)
        {
            var document = solution.GetDocument(typeLocation.SourceTree);
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var result = document;

            var declaration = syntaxRoot.FindNode(typeLocation.SourceSpan) as MemberDeclarationSyntax;
            if (declaration != null)
            {
                var newDeclaration = declaration;

                var actualTypeAccessibilityModifiers = GetAccessibilityModifiersFromMember(declaration);
                var hasAccessibilityModifiers = actualTypeAccessibilityModifiers.Any();

                var leadingTrivias = default(SyntaxTriviaList);
                var trailingTrivias = default(SyntaxTriviaList);
                if (!hasAccessibilityModifiers)
                {
                    var modifiers = declaration.GetModifiers();
                    if (modifiers.Count > 0)
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

                var newRoot = syntaxRoot.ReplaceNode(declaration, newDeclaration);

                result = document.WithSyntaxRoot(newRoot);
            }

            return result;
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
