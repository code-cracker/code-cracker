using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
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

            var inconsistentAccessibilityStrategy = GetInconsistentAccessibilityProvider(diagnostic);

            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var info = inconsistentAccessibilityStrategy.GetInconsistentAccessibilityInfo(syntaxRoot, diagnostic);

            var declarationForParameterType = FindMemberDeclarationInSyntaxRoot(syntaxRoot, info);

            if (declarationForParameterType != null)
            {
                context.RegisterCodeFix(CodeAction.Create(info.CodeActionMessage, c => ChangeTypeAccessibilityInDocument(context.Document, syntaxRoot, info.NewAccessibilityModifiers, declarationForParameterType)), diagnostic);
            }
        }

        private static InconsistentAccessibilityInfoProvider GetInconsistentAccessibilityProvider(Diagnostic diagnostic)
        {
            InconsistentAccessibilityInfoProvider inconsistentAccessibilityProvider = null;

            switch (diagnostic.Id)
            {
                case InconsistentAccessibilityInMethodParameterCompilerErrorNumber:
                    inconsistentAccessibilityProvider = new InconsistentAccessibilityInMethodParameter();
                    break;
            }

            return inconsistentAccessibilityProvider;
        }

        private static MemberDeclarationSyntax FindMemberDeclarationInSyntaxRoot(SyntaxNode syntaxRoot, InconsistentAccessibilityInfo info)
        {
            MemberDeclarationSyntax declarationForParameterType = FindTypeDeclarationInSyntaxRoot(syntaxRoot, info.InconsistentAccessibilityTypeName);
            if (declarationForParameterType == null)
            {
                declarationForParameterType = FindDelegateDeclarationInSyntaxRoot(syntaxRoot, info.InconsistentAccessibilityTypeName);
            }

            return declarationForParameterType;
        }

        private static BaseTypeDeclarationSyntax FindTypeDeclarationInSyntaxRoot(SyntaxNode root, string typeDeclarationIdentifierValueText) => root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().SingleOrDefault(typeDeclaration => string.Equals(typeDeclaration.Identifier.ValueText, typeDeclarationIdentifierValueText, StringComparison.Ordinal));

        private static DelegateDeclarationSyntax FindDelegateDeclarationInSyntaxRoot(SyntaxNode root, string delegateDeclarationIdentifierValueText) => root.DescendantNodes().OfType<DelegateDeclarationSyntax>().SingleOrDefault(declaration => string.Equals(declaration.Identifier.ValueText, delegateDeclarationIdentifierValueText, StringComparison.Ordinal));

        private static Task<Document> ChangeTypeAccessibilityInDocument(Document document, SyntaxNode syntaxRoot, SyntaxTokenList newAccessibilityModifiers, MemberDeclarationSyntax declaration)
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

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
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
