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
    [ExportCodeFixProvider("CodeCrackerInterfaceNameCodeFixProvider", LanguageNames.CSharp), Shared]
    public class InterfaceNameCodeFixProvider : CodeFixProvider
    {
        private enum FixType
        {
            PrivateFix,
            ProtectedFix
        }

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() => ImmutableArray.Create(InterfaceNameAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InterfaceDeclarationSyntax>().First();
            context.RegisterFix(CodeAction.Create("Consider start Interface name with letter 'I'", c => ChangeInterfaceNameAsync(context.Document, declaration, c, FixType.PrivateFix)), diagnostic);
        }

        private async Task<Document> ChangeInterfaceNameAsync(Document document, InterfaceDeclarationSyntax interfaceStatement, CancellationToken cancellationToken, FixType fixType)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var name = "I"+interfaceStatement.Identifier.ToString();

            var newInterface = SyntaxFactory.InterfaceDeclaration(interfaceStatement.AttributeLists,
                interfaceStatement.Modifiers,SyntaxFactory.Identifier(name),
                interfaceStatement.TypeParameterList,interfaceStatement.BaseList,
                interfaceStatement.ConstraintClauses,interfaceStatement.Members)
                .WithLeadingTrivia(interfaceStatement.GetLeadingTrivia())
                .WithTrailingTrivia(interfaceStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(interfaceStatement, newInterface);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}