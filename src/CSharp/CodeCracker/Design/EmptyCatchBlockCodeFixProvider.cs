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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(EmptyCatchBlockCodeFixProvider)), Shared]
    public class EmptyCatchBlockCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.EmptyCatchBlock.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Remove Empty Catch Block", c => RemoveEmptyCatchBlockAsync(context.Document, diagnostic, c), nameof(EmptyCatchBlockCodeFixProvider) + nameof(RemoveEmptyCatchBlockAsync)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Remove Empty Catch Block and Put a Documentation Link about Try...Catch use", c => RemoveEmptyCatchBlockPutCommentAsync(context.Document, diagnostic, c), nameof(EmptyCatchBlockCodeFixProvider ) + nameof(RemoveEmptyCatchBlockPutCommentAsync)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Insert Exception class to Catch", c => InsertExceptionClassCommentAsync(context.Document, diagnostic, c), nameof(EmptyCatchBlockCodeFixProvider) + nameof(InsertExceptionClassCommentAsync)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> RemoveEmptyCatchBlockAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken) =>
            await RemoveTryAsync(document, diagnostic, cancellationToken, insertComment: false);

        private async static Task<Document> RemoveEmptyCatchBlockPutCommentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken) =>
            await RemoveTryAsync(document, diagnostic, cancellationToken, insertComment: true);

        private async static Task<Document> RemoveTryAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken, bool insertComment)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var catchStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<CatchClauseSyntax>().First();
            var tryStatement = (TryStatementSyntax)catchStatement.Parent;
            var tryBlock = tryStatement.Block;
            if (insertComment)
            {
                tryBlock = tryBlock
                          .WithLeadingTrivia(tryBlock.GetLeadingTrivia())
                          .WithTrailingTrivia(SyntaxFactory.TriviaList(new SyntaxTrivia[] { tryBlock.GetTrailingTrivia().First(), SyntaxFactory.Comment("//TODO: Consider reading MSDN Documentation about how to use Try...Catch => http://msdn.microsoft.com/en-us/library/0yd65esw.aspx"), tryBlock.GetTrailingTrivia().Last() }))
                          .WithAdditionalAnnotations(Formatter.Annotation);
            }
            var newRoot = root.ReplaceNode(tryStatement, tryBlock);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private async static Task<Document> InsertExceptionClassCommentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var catchStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<CatchClauseSyntax>().First();
            var block = SyntaxFactory.Block(new StatementSyntax[] { SyntaxFactory.ThrowStatement() });
            var newCatch = SyntaxFactory.CatchClause().WithDeclaration(
                SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("Exception"))
                .WithIdentifier(SyntaxFactory.Identifier("ex")))
                .WithBlock(block)
                .WithLeadingTrivia(catchStatement.GetLeadingTrivia())
                .WithTrailingTrivia(catchStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(catchStatement, newCatch);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}