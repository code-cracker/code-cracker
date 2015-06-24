using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInIndexerParameter : InconsistentAccessibilityInfoProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInIndexerParameter_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var indexerThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).AncestorsAndSelf().OfType<IndexerDeclarationSyntax>().FirstOrDefault();
            if (indexerThatRaisedError != null)
            {
                var parameterType = ExtractParameterTypeFromDiagnosticMessage(diagnostic);

                result.TypeToChangeAccessibility = indexerThatRaisedError.ParameterList.Parameters.FindTypeInParametersList(parameterType);
                result.CodeActionMessage = string.Format(CodeActionMessage.ToString(), result.TypeToChangeAccessibility, indexerThatRaisedError.ParameterList.Parameters.ToString());
                result.NewAccessibilityModifiers = indexerThatRaisedError.Modifiers.CloneAccessibilityModifiers();
            }

            return result;
        }

        private static string ExtractParameterTypeFromDiagnosticMessage(Diagnostic diagnostic) =>
            Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture), "Inconsistent accessibility: parameter type '(.*)' is less accessible than indexer '(.*)'").Groups[1].Value;
    }
}
