using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInFieldType : InconsistentAccessibilityInfoProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInFieldType_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var fieldThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).AncestorsAndSelf().OfType<FieldDeclarationSyntax>().FirstOrDefault();
            if (fieldThatRaisedError != null)
            {
                result.TypeToChangeAccessibility = fieldThatRaisedError.Declaration.Type;
                var variableDeclarator = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).DescendantNodesAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
                result.CodeActionMessage = string.Format(CodeActionMessage.ToString(), result.TypeToChangeAccessibility, variableDeclarator.Identifier.ValueText);
                result.NewAccessibilityModifiers = fieldThatRaisedError.Modifiers.CloneAccessibilityModifiers();
            }

            return result;
        }
    }
}
