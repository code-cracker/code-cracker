using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInMethodParameter : InconsistentAccessibilityInfoProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInMethodParameter_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var methodThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).DescendantNodesAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            if (methodThatRaisedError != null)
            {
                var parameterTypeFromMessage = ExtractParameterTypeFromDiagnosticMessage(diagnostic);

                result.TypeToChangeAccessibility = methodThatRaisedError.ParameterList.Parameters.FindTypeInParametersList(parameterTypeFromMessage);
                result.CodeActionMessage = string.Format(CodeActionMessage.ToString(), parameterTypeFromMessage, methodThatRaisedError.GetIdentifier().ValueText);
                result.NewAccessibilityModifiers = methodThatRaisedError.CloneAccessibilityModifiers();
            }

            return result;
        }

        private static string ExtractParameterTypeFromDiagnosticMessage(Diagnostic diagnostic) =>
            Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture), "Inconsistent accessibility: parameter type '(.*)' is less accessible than method '(.*)'").Groups[1].Value;
    }
}
