using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInIndexerReturnType : InconsistentAccessibilitySourceProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInIndexerReturnType_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilitySource> ExtractInconsistentAccessibilitySourceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var indexerThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).AncestorsAndSelf().OfType<IndexerDeclarationSyntax>().FirstOrDefault();
            if (indexerThatRaisedError != null)
            {
                var type = indexerThatRaisedError.Type;
                var message = string.Format(CodeActionMessage.ToString(), type, indexerThatRaisedError.ParameterList.Parameters);

                return new InconsistentAccessibilitySource(message, type,
                    indexerThatRaisedError.Modifiers.CloneAccessibilityModifiers());
            }

            return InconsistentAccessibilitySource.Invalid;
        }
    }
}
