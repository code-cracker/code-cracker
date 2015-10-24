﻿using System;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class InconsistentAccessibilityInOperatorReturnType : InconsistentAccessibilityInfoProvider
    {
        private static readonly LocalizableString CodeActionMessage = new LocalizableResourceString(nameof(Resources.InconsistentAccessibilityInOperatorReturnType_Title), Resources.ResourceManager, typeof(Resources));

        public async Task<InconsistentAccessibilityInfo> GetInconsistentAccessibilityInfoAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var result = new InconsistentAccessibilityInfo();
            var syntaxRoot = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var nodeWhenErrorOccured = syntaxRoot.FindNode(diagnostic.Location.SourceSpan);
            var operatorThatRaisedError =
                nodeWhenErrorOccured.FirstAncestorOrSelfOfType(
                    typeof (OperatorDeclarationSyntax),
                    typeof (ConversionOperatorDeclarationSyntax)) as BaseMethodDeclarationSyntax;

            if (operatorThatRaisedError != null)
            {
                var type = GetTypeFromOperator(operatorThatRaisedError);
                result.TypeToChangeAccessibility = type;
                result.CodeActionMessage = string.Format(CodeActionMessage.ToString(), type, operatorThatRaisedError.GetOperatorName());
                result.NewAccessibilityModifiers = operatorThatRaisedError.Modifiers.CloneAccessibilityModifiers();
            }

            return result;
        }

        private static TypeSyntax GetTypeFromOperator(BaseMethodDeclarationSyntax operatorSyntax)
        {
            var result = default(TypeSyntax);
            switch (operatorSyntax.Kind())
            {
                case SyntaxKind.OperatorDeclaration:
                    result = ((OperatorDeclarationSyntax) operatorSyntax).ReturnType;
                    break;
                case SyntaxKind.ConversionOperatorDeclaration:
                    result = ((ConversionOperatorDeclarationSyntax) operatorSyntax).Type;
                    break;
            }

            return result;
        }
    }
}
