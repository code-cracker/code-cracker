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
    public class InconsistentAccessibilityInOperatorParameter : InconsistentAccessibilitySourceProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInOperatorParameter_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilitySource> ExtractInconsistentAccessibilitySourceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var operatorThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType(
                    typeof(OperatorDeclarationSyntax),
                    typeof(ConversionOperatorDeclarationSyntax)) as BaseMethodDeclarationSyntax;

            if (operatorThatRaisedError != null)
            {
                var parameterType = ExtractParameterTypeFromDiagnosticMessage(diagnostic);

                var message = string.Format(CodeActionMessage.ToString(), parameterType,
                    operatorThatRaisedError.GetOperatorName());

                return new InconsistentAccessibilitySource(message,
                    operatorThatRaisedError.ParameterList.Parameters.FindTypeInParametersList(parameterType),
                    operatorThatRaisedError.Modifiers.CloneAccessibilityModifiers());
            }

            return InconsistentAccessibilitySource.Invalid;
        }

        private static string ExtractParameterTypeFromDiagnosticMessage(Diagnostic diagnostic) =>
            Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture),
                "Inconsistent accessibility: parameter type '(.*)' is less accessible than operator '(.*)'").Groups[1]
                .Value;
    }
}
