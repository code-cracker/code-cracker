using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInDelegateParameterType : InconsistentAccessibilityInfoProvider
    {
        private static readonly LocalizableString CodeActionMessage =
    new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInDelegateParameter_Title),
        Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var delegateDeclarationThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType<DelegateDeclarationSyntax>();

            if (delegateDeclarationThatRaisedError != null)
            {
                var parameterType = ExtractParameterTypeFromDiagnosticMessage(diagnostic);

                result.TypeToChangeAccessibility =
                    delegateDeclarationThatRaisedError.ParameterList.Parameters.FindTypeInParametersList(parameterType);
                result.CodeActionMessage = string.Format(CodeActionMessage.ToString(), parameterType,
                    delegateDeclarationThatRaisedError.Identifier.Text);
                result.NewAccessibilityModifiers = delegateDeclarationThatRaisedError.Modifiers.CloneAccessibilityModifiers();
            }

            return result;
        }

        private static string ExtractParameterTypeFromDiagnosticMessage(Diagnostic diagnostic) =>
            Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture),
                "Inconsistent accessibility: parameter type '(.*)' is less accessible than delegate '(.*)'").Groups[1]
                .Value;
    }
}
