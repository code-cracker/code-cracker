using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInBaseInterface : InconsistentAccessibilityInfoProvider
    {
        private static readonly LocalizableString CodeActionMessage =
            new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInBaseInterface_Title),
                Resources.ResourceManager, typeof (Resources));

        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var interfaceDeclarationThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType<InterfaceDeclarationSyntax>();

            if (interfaceDeclarationThatRaisedError != null)
            {
                var baseInterface = ExtractBaseInterfaceFromDiagnosticMessage(diagnostic);

                result.TypeToChangeAccessibility =
                    interfaceDeclarationThatRaisedError.BaseList.Types.FindTypeInBaseTypesList(baseInterface);
                result.CodeActionMessage = string.Format(CodeActionMessage.ToString(), baseInterface,
                    interfaceDeclarationThatRaisedError.Identifier.ValueText);
                result.NewAccessibilityModifiers = interfaceDeclarationThatRaisedError.Modifiers.CloneAccessibilityModifiers();
            }

            return result;
        }

        private static string ExtractBaseInterfaceFromDiagnosticMessage(Diagnostic diagnostic) =>
    Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture),
        "Inconsistent accessibility: base interface '(.*)' is less accessible than interface '(.*)'").Groups[1]
        .Value;
    }
}
