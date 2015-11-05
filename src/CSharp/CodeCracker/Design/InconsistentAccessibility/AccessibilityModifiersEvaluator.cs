using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public sealed class AccessibilityModifiersEvaluator : IInconsistentAccessibilityFixInfoProvider
    {
        private readonly IInconsistentAccessibilityFixInfoProvider inconsistentAccessibilityFixInfoProvider;

        public AccessibilityModifiersEvaluator(IInconsistentAccessibilityFixInfoProvider inconsistentAccessibilityFixInfoProvider)
        {
            if (inconsistentAccessibilityFixInfoProvider == null)
            {
                throw new ArgumentNullException(nameof(inconsistentAccessibilityFixInfoProvider));
            }
            this.inconsistentAccessibilityFixInfoProvider = inconsistentAccessibilityFixInfoProvider;
        }

        public async Task<InconsistentAccessibilityFixInfo> CreateFixInfoAsync(CodeFixContext context, InconsistentAccessibilitySource source)
        {
            var fixInfo =
                await inconsistentAccessibilityFixInfoProvider.CreateFixInfoAsync(context, source).ConfigureAwait(false);

            if (source.Modifiers.Count == 1 && source.Modifiers.Single().Kind() == SyntaxKind.ProtectedKeyword)
            {
                var fixDestinationType =
                    await
                        FindTypeSymbolForAsync(context.Document, source.TypeToChangeAccessibility,
                            context.CancellationToken).ConfigureAwait(false);

                if (fixDestinationType.ContainingType == null)
                {
                    fixInfo = new InconsistentAccessibilityFixInfo(source.TypeToChangeAccessibility, Public());
                }
                else
                {
                    foreach (var diagnostic in context.Diagnostics)
                    {
                        var sourceContainingType =
                            await
                                SourceContainingType(context, diagnostic.Location.SourceSpan.Start)
                                    .ConfigureAwait(false);

                        if (SourceAndDestinationNotInTheSameType(sourceContainingType, fixDestinationType.ContainingType))
                        {
                            fixInfo = new InconsistentAccessibilityFixInfo(source.TypeToChangeAccessibility, Public());
                            break;
                        }
                    }
                }
            }

            return fixInfo;
        }

        private static async Task<ISymbol> FindTypeSymbolForAsync(Document document, TypeSyntax type, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var typeSymbol = ModelExtensions.GetSymbolInfo(semanticModel, type, cancellationToken).Symbol;

            return typeSymbol;
        }

        private static async Task<INamedTypeSymbol> SourceContainingType(CodeFixContext context, int position)
        {
            var semanticModel = await context.Document.GetSemanticModelAsync(context.CancellationToken);
            var symbol = semanticModel.GetEnclosingSymbol(position, context.CancellationToken) as INamedTypeSymbol;

            return symbol?.TypeKind != TypeKind.Class ? symbol?.ContainingType : symbol;
        }

        private static bool SourceAndDestinationNotInTheSameType(ISymbol source, ISymbol destination)
            => !destination.Equals(source);

        private static SyntaxTokenList Public() => SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
    }
}
