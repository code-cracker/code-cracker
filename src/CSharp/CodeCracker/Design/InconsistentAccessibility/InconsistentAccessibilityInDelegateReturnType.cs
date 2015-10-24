using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInDelegateReturnType : InconsistentAccessibilityInfoProvider
    {
        private static readonly LocalizableString CodeActionMessage =
            new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInDelegateReturnType_Title),
                Resources.ResourceManager, typeof (Resources));

        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document,
            Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var delegateDeclarationThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType<DelegateDeclarationSyntax>();

            if (delegateDeclarationThatRaisedError != null)
            {
                var type = delegateDeclarationThatRaisedError.ReturnType;
                result.TypeToChangeAccessibility = type;

                result.CodeActionMessage = string.Format(CodeActionMessage.ToString(),
                    delegateDeclarationThatRaisedError.ReturnType,
                    delegateDeclarationThatRaisedError.Identifier.ValueText);

                result.NewAccessibilityModifiers =
                    delegateDeclarationThatRaisedError.Modifiers.CloneAccessibilityModifiers();
            }

            return result;
        }
    }
}
