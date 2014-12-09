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

namespace CodeCracker.Style
{
    [ExportCodeFixProvider("CodeCrackerPropertyPrivateSetCodeFixProvider", LanguageNames.CSharp), Shared]
    public class PropertyPrivateSetCodeFixProvider : CodeFixProvider
    {
        public int MyProperty { get; set; }
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(PropertyPrivateSetAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();
            context.RegisterFix(CodeAction.Create("Consider use a 'private set'", c => ChangePropertySetAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> ChangePropertySetAsync(Document document, PropertyDeclarationSyntax propertyStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var current = propertyStatement;

            var getAcessor = (propertyStatement.AccessorList.Accessors[0].Keyword.Text == "get") ? propertyStatement.AccessorList.Accessors[0] : propertyStatement.AccessorList.Accessors[1];
            var setAcessor = (propertyStatement.AccessorList.Accessors[0].Keyword.Text == "set") ? propertyStatement.AccessorList.Accessors[0] : propertyStatement.AccessorList.Accessors[1];

            //var setAcessor = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration).WithModifiers(new SyntaxTokenList { SyntaxFactory.Token(SyntaxKind.PrivateKeyword) }); //.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)))});

            var privateModifier = SyntaxFactory.Token(SyntaxKind.PrivateKeyword)
                    .WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia(" "));

            var modifiers = setAcessor.Modifiers.Add(privateModifier);

            setAcessor = setAcessor.WithModifiers(modifiers);
                //.WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            var newProperty = SyntaxFactory.PropertyDeclaration(current.Type, current.Identifier)
                .WithModifiers(current.Modifiers)
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List<AccessorDeclarationSyntax>(new AccessorDeclarationSyntax[] { getAcessor, setAcessor })))
                .WithLeadingTrivia(propertyStatement.GetLeadingTrivia()).WithTrailingTrivia(propertyStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(propertyStatement, newProperty);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}