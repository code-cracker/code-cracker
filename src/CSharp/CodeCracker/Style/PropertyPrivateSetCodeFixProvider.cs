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
            context.RegisterCodeFix(CodeAction.Create("Change property to 'private set'", c => ChangePropertySetAsync(context.Document, diagnostic, c, FixType.PrivateFix)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Change property to 'protected set'", c => ChangePropertySetAsync(context.Document, diagnostic, c, FixType.ProtectedFix)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> ChangePropertySetAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken, FixType fixType)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var propertyStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var getAcessor = (propertyStatement.AccessorList.Accessors[0].Keyword.Text == "get") ? propertyStatement.AccessorList.Accessors[0] : propertyStatement.AccessorList.Accessors[1];
            var setAcessor = (propertyStatement.AccessorList.Accessors[0].Keyword.Text == "set") ? propertyStatement.AccessorList.Accessors[0] : propertyStatement.AccessorList.Accessors[1];

            var privateprotectedModifier = SyntaxFactory.Token(fixType == FixType.PrivateFix ? SyntaxKind.PrivateKeyword : SyntaxKind.ProtectedKeyword)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var modifiers = setAcessor.Modifiers.Add(privateprotectedModifier);
            setAcessor = setAcessor.WithModifiers(modifiers);

            var newProperty = SyntaxFactory.PropertyDeclaration(propertyStatement.Type, propertyStatement.Identifier)
                .WithModifiers(propertyStatement.Modifiers)
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List<AccessorDeclarationSyntax>(new AccessorDeclarationSyntax[] { getAcessor, setAcessor })))
                .WithLeadingTrivia(propertyStatement.GetLeadingTrivia()).WithTrailingTrivia(propertyStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(propertyStatement, newProperty);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}