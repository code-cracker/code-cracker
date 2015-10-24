using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class AccessibilityDomainChecker : IInconsistentAccessibilityCodeFix
    {
        private readonly IInconsistentAccessibilityCodeFix codeFix;

        public AccessibilityDomainChecker(IInconsistentAccessibilityCodeFix codeFix)
        {
            if (codeFix == null)
            {
                throw new ArgumentNullException(nameof(codeFix));
            }
            this.codeFix = codeFix;
        }

        public async Task FixAsync(CodeFixContext context, Diagnostic diagnostic,
            InconsistentAccessibilityInfo info)
        {
            var typeSymbol =
                await
                    FindTypeSymbolForAsync(context.Document, info.TypeToChangeAccessibility, context.CancellationToken);

            if (typeSymbol == null || IsTypeFromMetada(typeSymbol))
            {
                return;
            }

            if (ReasonForInconsistentAccessibilityIsInAccessibilityDomain(typeSymbol, info.NewAccessibilityModifiers))
            {
                foreach (
                    var inconsistentAccessibilityReason in
                        FindInconsistentAccessibilityReasons(typeSymbol, info.NewAccessibilityModifiers))
                {
                    var finder = new IdentifierNameFinder(inconsistentAccessibilityReason);
                    info.TypeToChangeAccessibility.Accept(finder);

                    var newInfo = new InconsistentAccessibilityInfo
                    {
                        CodeActionMessage = info.CodeActionMessage,
                        NewAccessibilityModifiers = info.NewAccessibilityModifiers,
                        TypeToChangeAccessibility = finder.Result
                    };

                    await codeFix.FixAsync(context, diagnostic, newInfo);
                }
            }
            else
            {
                await codeFix.FixAsync(context, diagnostic, info);
            }
        }

        private static bool IsTypeFromMetada(ISymbol typeSymbol) => typeSymbol.Locations.All(l => l.IsInMetadata);

        private static async Task<ISymbol> FindTypeSymbolForAsync(Document document, TypeSyntax type, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = semanticModel.GetSymbolInfo(type, cancellationToken).Symbol;

            return typeSymbol;
        }

        private static IEnumerable<string> FindInconsistentAccessibilityReasons(ISymbol typeSymbol, SyntaxTokenList accessibilityModifiers)
        {
            var symbolToCheck = typeSymbol;
            while (symbolToCheck != null)
            {
                if (IsInconsistentAccessibilityReason(symbolToCheck, accessibilityModifiers))
                {
                    yield return symbolToCheck.Name;
                }
                symbolToCheck = symbolToCheck.ContainingType;
            }
        }

        private static bool IsInconsistentAccessibilityReason(ISymbol typeSymbol, SyntaxTokenList accessibilityModifiers)
        {
            return typeSymbol.DeclaredAccessibility != ToSymbolAccessibility(accessibilityModifiers);
        }

        private static bool ReasonForInconsistentAccessibilityIsInAccessibilityDomain(ISymbol typeSymbol,
            SyntaxTokenList accessibilityModifiers)
            =>
                typeSymbol.DeclaredAccessibility == ToSymbolAccessibility(accessibilityModifiers) &&
                typeSymbol.ContainingType != null;

        private static Accessibility ToSymbolAccessibility(SyntaxTokenList accessibilityModifiers)
        {
            if (accessibilityModifiers.Count > 1)
            {
                return Accessibility.ProtectedAndInternal;
            }

            return MapToAccessibility(accessibilityModifiers.First().Kind());
        }

        private static Accessibility MapToAccessibility(SyntaxKind syntaxKind)
        {
            switch (syntaxKind)
            {
                case SyntaxKind.PublicKeyword:
                    return Accessibility.Public;
                case SyntaxKind.ProtectedKeyword:
                    return Accessibility.Protected;
                case SyntaxKind.InternalKeyword:
                    return Accessibility.Friend;
            }

            return Accessibility.NotApplicable;
        }

        private class IdentifierNameFinder : CSharpSyntaxWalker
        {
            private readonly string identifierNameToFind;

            public IdentifierNameFinder(string identifierNameToFind)
            {
                if (string.IsNullOrEmpty(identifierNameToFind))
                {
                    throw new ArgumentNullException(nameof(identifierNameToFind));
                }
                this.identifierNameToFind = identifierNameToFind;
            }

            public NameSyntax Result { get; private set; }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                if (Result == null &&
                    string.Equals(identifierNameToFind, node.Identifier.ValueText, StringComparison.Ordinal))
                {
                    Result = node;
                    return;
                }

                base.VisitIdentifierName(node);
            }

            public override void VisitQualifiedName(QualifiedNameSyntax node)
            {
                if (Result == null &&
                    string.Equals(node.Right.Identifier.Text, identifierNameToFind, StringComparison.Ordinal))
                {
                    Result = node;
                    return;
                }
                base.VisitQualifiedName(node);
            }
        }
    }
}
