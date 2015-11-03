using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInDelegateParameterType : InconsistentAccessibilitySourceProvider
    {
        private static readonly LocalizableString CodeActionMessage =
            new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInDelegateParameter_Title),
                Resources.ResourceManager, typeof (Resources));

        public async Task<InconsistentAccessibilitySource> ExtractInconsistentAccessibilitySourceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var delegateDeclarationThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType<DelegateDeclarationSyntax>();

            if (delegateDeclarationThatRaisedError != null)
            {
                var parameterType = ExtractParameterTypeFromDiagnosticMessage(diagnostic);

                var message = string.Format(CodeActionMessage.ToString(), parameterType,
                    delegateDeclarationThatRaisedError.Identifier.Text);

                return new InconsistentAccessibilitySource(message,
                    delegateDeclarationThatRaisedError.ParameterList.Parameters.FindTypeInParametersList(parameterType),
                    delegateDeclarationThatRaisedError.Modifiers.CloneAccessibilityModifiers());
            }

            return InconsistentAccessibilitySource.Invalid;
        }

        private static string ExtractParameterTypeFromDiagnosticMessage(Diagnostic diagnostic) =>
            Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture),
                "Inconsistent accessibility: parameter type '(.*)' is less accessible than delegate '(.*)'").Groups[1]
                .Value;
    }
}
