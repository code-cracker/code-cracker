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

namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider("CodeCrackerCatchEmptyCodeFixProvider", LanguageNames.CSharp), Shared]
    public class CatchEmptyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.CatchEmpty.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<CatchClauseSyntax>().First();
            context.RegisterCodeFix(CodeAction.Create("Add an Exception class", c => MakeCatchEmptyAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> MakeCatchEmptyAsync(Document document, CatchClauseSyntax catchStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var newCatch = SyntaxFactory.CatchClause().WithDeclaration(
                SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("Exception"))
                .WithIdentifier(SyntaxFactory.Identifier("ex")))
                .WithBlock(catchStatement.Block)
                .WithLeadingTrivia(catchStatement.GetLeadingTrivia())
                .WithTrailingTrivia(catchStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(catchStatement, newCatch);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}