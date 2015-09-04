using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyPrivateSetCodeFixProvider)), Shared]
    public class PropertyPrivateSetCodeFixProvider : CodeFixProvider
    {
        private enum FixType { PrivateFix, ProtectedFix }

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.PropertyPrivateSet.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Change property to 'private set'", c => ChangePropertySetAsync(context.Document, diagnostic, c, FixType.PrivateFix), nameof(PropertyPrivateSetCodeFixProvider) + nameof(FixType.PrivateFix)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Change property to 'protected set'", c => ChangePropertySetAsync(context.Document, diagnostic, c, FixType.ProtectedFix), nameof(PropertyPrivateSetCodeFixProvider) + nameof(FixType.ProtectedFix)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> ChangePropertySetAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken, FixType fixType)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var propertyStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var setAcessor = (propertyStatement.AccessorList.Accessors[0].Keyword.Text == "set") ? propertyStatement.AccessorList.Accessors[0] : propertyStatement.AccessorList.Accessors[1];
            var privateprotectedModifier = SyntaxFactory.Token(fixType == FixType.PrivateFix ? SyntaxKind.PrivateKeyword : SyntaxKind.ProtectedKeyword)
                    .WithTrailingTrivia(setAcessor.GetTrailingTrivia())
                    .WithLeadingTrivia(setAcessor.GetLeadingTrivia());

            var newSet = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration, setAcessor.AttributeLists, SyntaxTokenList.Create(privateprotectedModifier), setAcessor.Body);
            if(setAcessor.Body == null) newSet = newSet.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            var newProperty = propertyStatement.ReplaceNode(setAcessor, newSet);

            var newRoot = root.ReplaceNode(propertyStatement, newProperty);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}