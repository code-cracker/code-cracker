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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CatchEmptyCodeFixProvider)), Shared]
    public class CatchEmptyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.CatchEmpty.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Add an Exception class", c => MakeCatchEmptyAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> MakeCatchEmptyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var catchStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<CatchClauseSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var newCatch = SyntaxFactory.CatchClause().WithDeclaration(
                SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("Exception"))
                .WithIdentifier(SyntaxFactory.Identifier("ex")))
                .WithBlock(catchStatement.Block)
                .WithLeadingTrivia(catchStatement.GetLeadingTrivia())
                .WithTrailingTrivia(catchStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(catchStatement, newCatch);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}