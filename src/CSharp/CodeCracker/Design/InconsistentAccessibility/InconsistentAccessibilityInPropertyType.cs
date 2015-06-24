using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInPropertyType : InconsistentAccessibilityInfoProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInPropertyType_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var propertyThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (propertyThatRaisedError != null)
            {
                result.TypeToChangeAccessibility = propertyThatRaisedError.Type;
                result.CodeActionMessage = string.Format(CodeActionMessage.ToString(), result.TypeToChangeAccessibility, propertyThatRaisedError.Identifier.ValueText);
                result.NewAccessibilityModifiers = propertyThatRaisedError.Modifiers.CloneAccessibilityModifiers();
            }

            return result;
        }
    }
}
