using System.Globalization;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInBaseClass : InconsistentAccessibilitySourceProvider
    {
        private static readonly LocalizableString CodeActionMessage =
            new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInBaseClass_Title),
                Resources.ResourceManager, typeof (Resources));

        public async Task<InconsistentAccessibilitySource> ExtractInconsistentAccessibilitySourceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var classDeclarationThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType<ClassDeclarationSyntax>();

            if (classDeclarationThatRaisedError != null)
            {
                var baseClass = ExtractBaseClassFromDiagnosticMessage(diagnostic);

                var message = string.Format(CodeActionMessage.ToString(), baseClass,
                    classDeclarationThatRaisedError.Identifier.ValueText);

                return new InconsistentAccessibilitySource(message,
                    classDeclarationThatRaisedError.BaseList.Types.FindTypeInBaseTypesList(baseClass),
                    classDeclarationThatRaisedError.Modifiers.CloneAccessibilityModifiers());
            }

            return InconsistentAccessibilitySource.Invalid;
        }

        private static string ExtractBaseClassFromDiagnosticMessage(Diagnostic diagnostic) =>
            Regex.Match(diagnostic.GetMessage(CultureInfo.InvariantCulture),
                "Inconsistent accessibility: base class '(.*)' is less accessible than class '(.*)'").Groups[1]
                .Value;
    }
}
