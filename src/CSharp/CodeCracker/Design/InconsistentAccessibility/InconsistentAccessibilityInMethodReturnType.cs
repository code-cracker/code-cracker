using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInMethodReturnType : InconsistentAccessibilitySourceProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInMethodReturnType_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilitySource> ExtractInconsistentAccessibilitySourceAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var methodThatRaisedError = syntaxRoot.FindNode(diagnostic.Location.SourceSpan).DescendantNodesAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodThatRaisedError != null)
            {
                var message = string.Format(CodeActionMessage.ToString(), methodThatRaisedError.ReturnType,
                    methodThatRaisedError.GetIdentifier().ValueText);

                return
                    new InconsistentAccessibilitySource(
                        message, methodThatRaisedError.ReturnType,
                        methodThatRaisedError.Modifiers.CloneAccessibilityModifiers());
            }

            return InconsistentAccessibilitySource.Invalid;
        }
    }
}
