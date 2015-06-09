using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInMethodParameter : InconsistentAccessibilityInfoProvider
    {
        public InconsistentAccessibilityInfo GetInconsistentAccessibilityInfo(SyntaxNode syntaxRoot, Diagnostic diagnostic)
        {
            var result = new InconsistentAccessibilityInfo();

            var methodThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).DescendantNodesAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            if (methodThatRaisedError != null)
            {
                var parameterTypeFromMessage = ExtractParameterTypeFromErrorMessage(diagnostic.ToString());

                var parameterTypeName = parameterTypeFromMessage;
                var parameterTypeDotIndex = parameterTypeName.LastIndexOf('.');
                if (parameterTypeDotIndex > 0)
                {
                    parameterTypeName = parameterTypeName.Substring(parameterTypeDotIndex + 1);
                }

                result.CodeActionMessage = $"Change parameter type '{parameterTypeFromMessage}' accessibility to be as accessible as method '{GetMethodIdentifierValueText(methodThatRaisedError)}'";
                result.NewAccessibilityModifiers = CreateAccessibilityModifiersFromMethod(methodThatRaisedError);
                result.InconsistentAccessibilityTypeName = parameterTypeName;
            }

            return result;
        }

        private static string ExtractParameterTypeFromErrorMessage(string compilerErrorMessage)
        {
            const int parameterTypeStartShift = 52;

            var parameterTypeNameStart = compilerErrorMessage.IndexOf(InconsistentAccessibilityCodeFixProvider.InconsistentAccessibilityInMethodParameterCompilerErrorNumber, StringComparison.Ordinal) + parameterTypeStartShift;
            var parameterTypeNameLength = compilerErrorMessage.IndexOf('\'', parameterTypeNameStart) - parameterTypeNameStart;
            return compilerErrorMessage.Substring(parameterTypeNameStart, parameterTypeNameLength);
        }

        private static string GetMethodIdentifierValueText(BaseMethodDeclarationSyntax method) => method.IsKind(SyntaxKind.MethodDeclaration) ? ((MethodDeclarationSyntax)method).Identifier.ValueText : ((ConstructorDeclarationSyntax)method).Identifier.ValueText;

        private static SyntaxTokenList CreateAccessibilityModifiersFromMethod(BaseMethodDeclarationSyntax method)
        {
            var modifiers = method.Modifiers;
            if (method.Parent.IsKind(SyntaxKind.InterfaceDeclaration))
            {
                modifiers = ((InterfaceDeclarationSyntax)method.Parent).Modifiers;
            }

            var accessibilityModifiers = modifiers.Where(token => token.IsKind(SyntaxKind.PublicKeyword, SyntaxKind.ProtectedKeyword, SyntaxKind.InternalKeyword)).Select(token => SyntaxFactory.Token(token.Kind()));

            return SyntaxFactory.TokenList(EnsureProtectedBeforeInternal(accessibilityModifiers));
        }

        private static IEnumerable<SyntaxToken> EnsureProtectedBeforeInternal(IEnumerable<SyntaxToken> modifiers) => modifiers.OrderByDescending(token => token.RawKind);
    }
}
