using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInPropertyType : InconsistentAccessibilitySourceProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInPropertyType_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilitySource> ExtractInconsistentAccessibilitySourceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var propertyThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().FirstOrDefault();
            if (propertyThatRaisedError != null)
            {
                var type = propertyThatRaisedError.Type;

                var message = string.Format(CodeActionMessage.ToString(), type, propertyThatRaisedError.Identifier.ValueText);

                return
                    new InconsistentAccessibilitySource(
                        message,
                        type, propertyThatRaisedError.Modifiers.CloneAccessibilityModifiers());
            }

            return InconsistentAccessibilitySource.Invalid;
        }
    }
}
