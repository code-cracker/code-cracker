using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerRethrowExceptionCodeFixProvider", LanguageNames.CSharp), Shared]
    public class RethrowExceptionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(RethrowExceptionAnalyzer.DiagnosticId);
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
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ThrowStatementSyntax>().First();
            context.RegisterFix(CodeAction.Create("Rethrow as inner exception", c => MakeThrowAsInnerAsync(context.Document, declaration, c)), diagnostic);
            context.RegisterFix(CodeAction.Create("Throw original exception", c => MakeThrowAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> MakeThrowAsync(Document document, ThrowStatementSyntax throwStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);


            var newThrow = (ThrowStatementSyntax)SyntaxFactory.ParseStatement("throw;")
                .WithLeadingTrivia(throwStatement.GetLeadingTrivia())
                .WithTrailingTrivia(throwStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(throwStatement, newThrow);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;

        }

        private async Task<Document> MakeThrowAsInnerAsync(Document document, ThrowStatementSyntax throwStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            //var exceptionType = SyntaxFactory.ParseExpression("System.Exception").WithAdditionalAnnotations(Simplifier.Annotation);
            //var newThrow = (ThrowStatementSyntax)SyntaxFactory.ThrowStatement(SyntaxFactory.ObjectCreationExpression(exceptionType, SyntaxFactory.ArgumentList(new SeparatedSyntaxList<ArgumentSyntax>() { SyntaxFactory.Argument(SyntaxFactory.ParseExpression("ex")) }), SyntaxFactory.InitializerExpression(SyntaxKind.ObjectCreationExpression)));
            var newThrow = (ThrowStatementSyntax)SyntaxFactory.ParseStatement("throw new Exception(\"some reason to rethrow\", ex);")
                .WithLeadingTrivia(throwStatement.GetLeadingTrivia())
                .WithTrailingTrivia(throwStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(throwStatement, newThrow);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;

        }
    }
}