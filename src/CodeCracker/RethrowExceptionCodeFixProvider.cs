using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

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
            var ident = throwStatement.Expression as IdentifierNameSyntax;
            var exSymbol = semanticModel.GetSymbolInfo(ident).Symbol as ILocalSymbol;
            var exceptionType = SyntaxFactory.ParseTypeName("System.Exception").WithAdditionalAnnotations(Simplifier.Annotation);
            var objectCreationExpressionSyntax = SyntaxFactory.ObjectCreationExpression(exceptionType).WithArgumentList(
                                    SyntaxFactory.ArgumentList(
                                        SyntaxFactory.SeparatedList(
                                        new[]
                                            {
                                                    SyntaxFactory.Argument(SyntaxFactory.ParseExpression("\"some reason to rethrow\"")),
                                                    SyntaxFactory.Argument(SyntaxFactory.ParseExpression(exSymbol.Name))
                                            })));
            var newThrow = SyntaxFactory.ThrowStatement(objectCreationExpressionSyntax)
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