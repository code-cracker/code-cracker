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
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InconsistentAccessibilityCodeFixProvider)), Shared]
    public sealed class InconsistentAccessibilityCodeFixProvider : CodeFixProvider
    {
        private const string InconsistentAccessibilityInMethodParameterCompilerErrorNumber = "CS0051";

        public static readonly string MessageFormat = "Change parameter type '{0}' accessibility to be as accessible as method '{1}'";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(InconsistentAccessibilityInMethodParameterCompilerErrorNumber);

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var syntaxRoot = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var methodThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).DescendantNodesAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();

            if (methodThatRaisedError != null)
            {
                var parameterTypeFromMessage = ExtractParameterTypeFromErrorMessage(diagnostic.ToString());

                var parameterTypeName = parameterTypeFromMessage;
                var parameterTypeDotIndex = parameterTypeName.LastIndexOf('.');
                if(parameterTypeDotIndex > 0)
                {
                    parameterTypeName = parameterTypeName.Substring(parameterTypeDotIndex + 1);
                }

                MemberDeclarationSyntax declarationForParameterType = FindTypeDeclarationInSyntaxRoot(syntaxRoot, parameterTypeName);
                if (declarationForParameterType == null)
                {
                    declarationForParameterType = FindDelegateDeclarationInSyntaxRoot(syntaxRoot, parameterTypeName);
                }

                if (declarationForParameterType != null)
                {
                    var message = string.Format(MessageFormat, parameterTypeFromMessage, GetMethodIdentifierValueText(methodThatRaisedError));
                    context.RegisterCodeFix(CodeAction.Create(message, c => ChangeTypeAccessibilityInDocument(context.Document, syntaxRoot, methodThatRaisedError, declarationForParameterType)), diagnostic);
                }
            }
        }

        private static string ExtractParameterTypeFromErrorMessage(string compilerErrorMessage)
        {
            const int parameterTypeStartShift = 52;

            var parameterTypeNameStart = compilerErrorMessage.IndexOf(InconsistentAccessibilityInMethodParameterCompilerErrorNumber, StringComparison.Ordinal) + parameterTypeStartShift;
            var parameterTypeNameLength = compilerErrorMessage.IndexOf('\'', parameterTypeNameStart) - parameterTypeNameStart;
            return compilerErrorMessage.Substring(parameterTypeNameStart, parameterTypeNameLength);
        }

        private static string GetMethodIdentifierValueText(BaseMethodDeclarationSyntax method) => method.IsKind(SyntaxKind.MethodDeclaration) ? ((MethodDeclarationSyntax)method).Identifier.ValueText : ((ConstructorDeclarationSyntax)method).Identifier.ValueText;

        private static BaseTypeDeclarationSyntax FindTypeDeclarationInSyntaxRoot(SyntaxNode root, string typeDeclarationIdentifierValueText) => root.DescendantNodes().OfType<BaseTypeDeclarationSyntax>().SingleOrDefault(typeDeclaration => string.Equals(typeDeclaration.Identifier.ValueText, typeDeclarationIdentifierValueText, StringComparison.Ordinal));

        private static DelegateDeclarationSyntax FindDelegateDeclarationInSyntaxRoot(SyntaxNode root, string delegateDeclarationIdentifierValueText) => root.DescendantNodes().OfType<DelegateDeclarationSyntax>().SingleOrDefault(declaration => string.Equals(declaration.Identifier.ValueText, delegateDeclarationIdentifierValueText, StringComparison.Ordinal));

        private static Task<Document> ChangeTypeAccessibilityInDocument(Document document, SyntaxNode syntaxRoot, BaseMethodDeclarationSyntax method, MemberDeclarationSyntax declaration)
        {
            MemberDeclarationSyntax newDeclaration = null;
            var newAccessibilityModifiers = CreateAccessibilityModifiersFromMethod(method);

            var actualTypeAccessibilityModifiers = GetAccessibilityModifiersFromMember(declaration);
            var hasAccessibilityModifiers = actualTypeAccessibilityModifiers.Any();

            var leadingTrivias = default(SyntaxTriviaList);
            var trailingTrivias = default(SyntaxTriviaList);
            if (!hasAccessibilityModifiers)
            {
                var modifiers = GetModifiersForMember(declaration);
                if (modifiers.Count > 0)
                {
                    var firstModifier = modifiers.First();
                    leadingTrivias = firstModifier.LeadingTrivia;
                    newDeclaration = ChangeDeclarationModifiers(declaration, modifiers.Replace(firstModifier, firstModifier.WithLeadingTrivia(default(SyntaxTriviaList))));
                }
                else
                {
                    leadingTrivias = declaration.GetLeadingTrivia();
                    newDeclaration = declaration.WithoutLeadingTrivia();
                }
                trailingTrivias = SyntaxFactory.TriviaList(SyntaxFactory.Space);
            }
            else
            {
                leadingTrivias = actualTypeAccessibilityModifiers.First().LeadingTrivia;
                trailingTrivias = SyntaxFactory.TriviaList(actualTypeAccessibilityModifiers.Skip(1).SelectMany(token => token.LeadingTrivia).Union(actualTypeAccessibilityModifiers.SelectMany(token => token.TrailingTrivia)));
            }

            newAccessibilityModifiers = ChangeTriviasForModifiers(newAccessibilityModifiers, leadingTrivias, trailingTrivias);

            newDeclaration = ChangeDeclarationModifiers(newDeclaration ?? declaration, newAccessibilityModifiers);

            var newRoot = syntaxRoot.ReplaceNode(declaration, newDeclaration);

            return Task.FromResult(document.WithSyntaxRoot(newRoot));
        }

        private static SyntaxTokenList CreateAccessibilityModifiersFromMethod(BaseMethodDeclarationSyntax method)
        {
            var modifiers = method.Modifiers;
            if(method.Parent.IsKind(SyntaxKind.InterfaceDeclaration))
            {
                modifiers = ((InterfaceDeclarationSyntax)method.Parent).Modifiers;
            }

            var accessibilityModifiers = modifiers.Where(token => token.IsKind(SyntaxKind.PublicKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword)).Select(token => SyntaxFactory.Token(token.Kind()));

            return SyntaxFactory.TokenList(EnsureProtectedBeforeInternal(accessibilityModifiers));
        }

        private static IEnumerable<SyntaxToken> EnsureProtectedBeforeInternal(IEnumerable<SyntaxToken> modifiers) => modifiers.OrderByDescending(token => token.RawKind);

        private static IEnumerable<SyntaxToken> GetAccessibilityModifiersFromMember(MemberDeclarationSyntax member) => GetModifiersForMember(member).Where(token => token.IsKind(SyntaxKind.PublicKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword, SyntaxKind.PrivateKeyword));


        private static SyntaxTokenList GetModifiersForMember(MemberDeclarationSyntax member)
        {
            var typeDeclaration = member as BaseTypeDeclarationSyntax;

            if (typeDeclaration != null)
            {
                return typeDeclaration.Modifiers;
            }

            var delegateDeclaration = member as DelegateDeclarationSyntax;
            if (delegateDeclaration != null)
            {
                return delegateDeclaration.Modifiers;
            }

            return default(SyntaxTokenList);
        }

        private static MemberDeclarationSyntax ChangeDeclarationModifiers(MemberDeclarationSyntax declaration, SyntaxTokenList modifiers)
        {
            var result = declaration;

            switch(declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    result = ((ClassDeclarationSyntax)declaration).WithModifiers(modifiers);
                    break;
                case SyntaxKind.StructDeclaration:
                    result = ((StructDeclarationSyntax)declaration).WithModifiers(modifiers);
                    break;
                case SyntaxKind.InterfaceDeclaration:
                    result = ((InterfaceDeclarationSyntax)declaration).WithModifiers(modifiers);
                    break;
                case SyntaxKind.EnumDeclaration:
                    result = ((EnumDeclarationSyntax)declaration).WithModifiers(modifiers);
                    break;
                case SyntaxKind.DelegateDeclaration:
                    result = ((DelegateDeclarationSyntax)declaration).WithModifiers(modifiers);
                    break;
            }

            return result;
        }

        private static SyntaxTokenList ChangeTriviasForModifiers(SyntaxTokenList modifiers, SyntaxTriviaList leadingTrivias, SyntaxTriviaList trailingTrivias)
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
