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

namespace CodeCracker.Design
{
    [ExportCodeFixProvider("EmptyCatchBlockCodeFixProvider", LanguageNames.CSharp), Shared]
    public class EmptyCatchBlockCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(EmptyCatchBlockAnalyzer.DiagnosticId);
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
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<CatchClauseSyntax>().First();
            context.RegisterFix(CodeAction.Create("Remove Empty Catch Block", c => RemoveEmptyCatchBlockAsync(context.Document, declaration, c)), diagnostic);
            context.RegisterFix(CodeAction.Create("Remove Empty Catch Block and Put a Documentation Link about Try...Catch use", c => RemoveEmptyCatchBlockPutCommentAsync(context.Document, declaration, c)), diagnostic);
            context.RegisterFix(CodeAction.Create("Insert Exception class to Catch", c => InsertExceptionClassCommentAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> RemoveTry(Document document, CatchClauseSyntax catchStatement, bool insertComment = false)
        {
            var tryStatement = (TryStatementSyntax)catchStatement.Parent;
            var tryBlock = tryStatement.Block;

            if (insertComment)
            {
                tryBlock = tryBlock
                          .WithLeadingTrivia(tryBlock.GetLeadingTrivia())
                          .WithTrailingTrivia(SyntaxFactory.TriviaList(new SyntaxTrivia[] { tryBlock.GetTrailingTrivia().First(), SyntaxFactory.Comment("//TODO: Consider reading MSDN Documentation about how to use Try...Catch => http://msdn.microsoft.com/en-us/library/0yd65esw.aspx"), tryBlock.GetTrailingTrivia().Last() }))
                          .WithAdditionalAnnotations(Formatter.Annotation);
            }
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(tryStatement, (SyntaxNode)tryBlock);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private async Task<Document> RemoveEmptyCatchBlockAsync(Document document, CatchClauseSyntax catchStatement, CancellationToken cancellationToken)
        {
            return await RemoveTry(document, catchStatement);
        }

        private async Task<Document> RemoveEmptyCatchBlockPutCommentAsync(Document document, CatchClauseSyntax catchStatement, CancellationToken cancellationToken)
        {
            return await RemoveTry(document, catchStatement, true);
        }

        private async Task<Document> InsertExceptionClassCommentAsync(Document document, CatchClauseSyntax catchStatement, CancellationToken cancellationToken)
        {
            var block = SyntaxFactory.Block(new StatementSyntax[] { SyntaxFactory.ThrowStatement() });

            var newCatch = SyntaxFactory.CatchClause().WithDeclaration(
                SyntaxFactory.CatchDeclaration(SyntaxFactory.IdentifierName("Exception"))
                .WithIdentifier(SyntaxFactory.Identifier("ex")))
                .WithBlock(block)
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