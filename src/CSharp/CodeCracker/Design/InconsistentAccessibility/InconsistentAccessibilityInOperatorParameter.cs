using System;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public class InconsistentAccessibilityInOperatorParameter : InconsistentAccessibilityInfoProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInOperatorParameter_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var operatorThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType(
                    typeof(OperatorDeclarationSyntax),
                    typeof(ConversionOperatorDeclarationSyntax)) as BaseMethodDeclarationSyntax;

            if (operatorThatRaisedError != null)
            {
                var parameterType = ExtractParameterTypeFromDiagnosticMessage(diagnostic);

                result.TypeToChangeAccessibility = operatorThatRaisedError.ParameterList.Parameters.FindTypeInParametersList(parameterType);
                result.CodeActionMessage = string.Format(CodeActionMessage.ToString(), parameterType,
                    operatorThatRaisedError.GetOperatorName());
                result.NewAccessibilityModifiers = operatorThatRaisedError.Modifiers.CloneAccessibilityModifiers();
            }

            return result;
        }

        private static string ExtractParameterTypeFromDiagnosticMessage(Diagnostic diagnostic) =>
            Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture),
                "Inconsistent accessibility: parameter type '(.*)' is less accessible than operator '(.*)'").Groups[1]
                .Value;
    }
}
