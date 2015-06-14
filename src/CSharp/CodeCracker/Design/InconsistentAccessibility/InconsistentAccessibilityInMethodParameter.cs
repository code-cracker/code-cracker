using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using System.Globalization;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInMethodParameter : InconsistentAccessibilityInfoProvider
    {
        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var methodThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).DescendantNodesAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            if (methodThatRaisedError != null)
            {
                var parameterTypeFromMessage = ExtractParameterTypeFromDiagnosticMessage(diagnostic);

                var parameterTypeLastIdentifier = parameterTypeFromMessage;
                var parameterTypeDotIndex = parameterTypeLastIdentifier.LastIndexOf('.');
                if (parameterTypeDotIndex > 0)
                {
                    parameterTypeLastIdentifier = parameterTypeLastIdentifier.Substring(parameterTypeDotIndex + 1);
                }

                result.TypeToChangeAccessibility = FindTypeSyntaxFromParametersList(methodThatRaisedError.ParameterList.Parameters, parameterTypeLastIdentifier);
                result.CodeActionMessage = $"Change parameter type '{parameterTypeFromMessage}' accessibility to be as accessible as method '{GetMethodIdentifierValueText(methodThatRaisedError)}'";
                result.NewAccessibilityModifiers = CreateAccessibilityModifiersFromMethod(methodThatRaisedError);
            }

            return result;
        }

        private static string ExtractParameterTypeFromDiagnosticMessage(Diagnostic diagnostic) =>
            Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture), "Inconsistent accessibility: parameter type '(.*)' is less accessible than method '(.*)'").Groups[1].Value;

        private static TypeSyntax FindTypeSyntaxFromParametersList(SeparatedSyntaxList<ParameterSyntax> parameterList, string typeName)
        {
            TypeSyntax result = null;
            foreach(var parameter in parameterList)
            {
                var valueText = GetLastIdentifierValueText(parameter.Type);

                if (!string.IsNullOrEmpty(valueText))
                {
                    if (string.Equals(valueText, typeName, StringComparison.Ordinal))
                    {
                        result = parameter.Type;
                        break;
                    }
                }
            }

            return result;
        }

        private static string GetLastIdentifierValueText(CSharpSyntaxNode node)
        {
            var result = string.Empty;
            switch (node.Kind())
            {
                case SyntaxKind.IdentifierName:
                    result = ((IdentifierNameSyntax)node).Identifier.ValueText;
                    break;
                case SyntaxKind.QualifiedName:
                    result = GetLastIdentifierValueText(((QualifiedNameSyntax)node).Right);
                    break;
                case SyntaxKind.GenericName:
                    var genericNameSyntax = ((GenericNameSyntax)node);
                    result = $"{genericNameSyntax.Identifier.ValueText}{genericNameSyntax.TypeArgumentList.ToString()}";
                    break;
                case SyntaxKind.AliasQualifiedName:
                    result = ((AliasQualifiedNameSyntax)node).Name.Identifier.ValueText;
                    break;
            }

            return result;
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
