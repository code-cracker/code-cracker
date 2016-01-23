using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInBaseInterface : InconsistentAccessibilitySourceProvider
    {
        private static readonly LocalizableString CodeActionMessage =
            new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInBaseInterface_Title),
                Resources.ResourceManager, typeof (Resources));

        public async Task<InconsistentAccessibilitySource> ExtractInconsistentAccessibilitySourceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var interfaceDeclarationThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType<InterfaceDeclarationSyntax>();

            if (interfaceDeclarationThatRaisedError != null)
            {
                var baseInterface = ExtractBaseInterfaceFromDiagnosticMessage(diagnostic);

                var message = string.Format(CodeActionMessage.ToString(), baseInterface,
                    interfaceDeclarationThatRaisedError.Identifier.ValueText);

                return new InconsistentAccessibilitySource(message,
                    interfaceDeclarationThatRaisedError.BaseList.Types.FindTypeInBaseTypesList(baseInterface),
                    interfaceDeclarationThatRaisedError.Modifiers.CloneAccessibilityModifiers());
            }

            return InconsistentAccessibilitySource.Invalid;
        }

        private static string ExtractBaseInterfaceFromDiagnosticMessage(Diagnostic diagnostic) =>
    Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture),
        "Inconsistent accessibility: base interface '(.*)' is less accessible than interface '(.*)'").Groups[1]
        .Value;
    }
}
