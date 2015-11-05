using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.CSharp.Design.InconsistentAccessibility;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.Test.CSharp.Design.InconsistentAccessibilityTests
{
    public partial class InconsistentAccessibilityTests : CodeFixVerifier
    {
        private async Task<InconsistentAccessibilityFixInfo> AccessibilityDomainCheckerFixInfoFor(string testCode)
        {
            var document = CreateDocument(testCode, LanguageNames.CSharp, LanguageVersion.CSharp6);
            var syntaxRoot = await document.GetSyntaxRootAsync();
            var descendants = syntaxRoot.DescendantNodesAndSelf();
            var diagnostic = CreateCs0059DiagnosticForDelegateDeclaration(descendants);
            var typeSyntax = GetTypeSyntaxFromFirstParameterInDelegateDeclaration(descendants);

            var context = new CodeFixContext(document, diagnostic, (action, array) => { }, CancellationToken.None);
            return
                await
                    accessibilityDomainChecker.CreateFixInfoAsync(context,
                        CreateSourceFor(typeSyntax,
                            SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.PublicKeyword))))
                        .ConfigureAwait(false);
        }

        private async Task<InconsistentAccessibilityFixInfo> AccessibilityModifiersEvaluatorFixInfoFor(string testCode, SyntaxTokenList modifiers)
        {
            var document = CreateDocument(testCode, LanguageNames.CSharp, LanguageVersion.CSharp6);
            var syntaxRoot = await document.GetSyntaxRootAsync();
            var descendants = syntaxRoot.DescendantNodesAndSelf();
            var diagnostic = CreateCs0059DiagnosticForDelegateDeclaration(descendants);
            var typeSyntax = GetTypeSyntaxFromFirstParameterInDelegateDeclaration(descendants);

            var context = new CodeFixContext(document, diagnostic, (action, array) => { }, CancellationToken.None);
            return
                await
                    accessibilityModifiersEvaluator.CreateFixInfoAsync(context,
                        CreateSourceFor(typeSyntax, modifiers))
                        .ConfigureAwait(false);
        }

        private static InconsistentAccessibilitySource CreateSourceFor(TypeSyntax typeSyntax,
            SyntaxTokenList sourceModifiers)
            =>
                new InconsistentAccessibilitySource(string.Empty, typeSyntax,
                    sourceModifiers);

        private static Diagnostic CreateCs0059DiagnosticForDelegateDeclaration(IEnumerable<SyntaxNode> descendants)
        {
            var delegateDeclaration = descendants.OfType<DelegateDeclarationSyntax>().Single();

            return
                Diagnostic.Create(
                    new DiagnosticDescriptor("CS0059", "Title", "Format", "Category", DiagnosticSeverity.Error, true),
                    delegateDeclaration.Identifier.GetLocation());
        }

        private static TypeSyntax GetTypeSyntaxFromFirstParameterInDelegateDeclaration(
            IEnumerable<SyntaxNode> descendants) =>
                descendants.OfType<DelegateDeclarationSyntax>().Single().ParameterList.Parameters.First().Type;
    }
}
