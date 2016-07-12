using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInDelegateReturnType : InconsistentAccessibilitySourceProvider
    {
        private static readonly LocalizableString CodeActionMessage =
            new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInDelegateReturnType_Title),
                Resources.ResourceManager, typeof (Resources));

        public async Task<InconsistentAccessibilitySource> ExtractInconsistentAccessibilitySourceAsync(Document document,
            Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var delegateDeclarationThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType<DelegateDeclarationSyntax>();

            if (delegateDeclarationThatRaisedError != null)
            {
                var type = delegateDeclarationThatRaisedError.ReturnType;

                return new InconsistentAccessibilitySource(string.Format(CodeActionMessage.ToString(),
                    delegateDeclarationThatRaisedError.ReturnType,
                    delegateDeclarationThatRaisedError.Identifier.ValueText), type,
                    delegateDeclarationThatRaisedError.Modifiers.CloneAccessibilityModifiers());
            }

            return InconsistentAccessibilitySource.Invalid;
        }
    }
}
