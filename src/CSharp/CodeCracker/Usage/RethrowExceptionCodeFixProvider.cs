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

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RethrowExceptionCodeFixProvider)), Shared]
    public class RethrowExceptionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.RethrowException.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Rethrow as inner exception", c => MakeThrowAsInnerAsync(context.Document, diagnostic, c)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Throw original exception", c => MakeThrowAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> MakeThrowAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var throwStatement = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<ThrowStatementSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var newThrow = (ThrowStatementSyntax)SyntaxFactory.ParseStatement("throw;")
                .WithLeadingTrivia(throwStatement.GetLeadingTrivia())
                .WithTrailingTrivia(throwStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(throwStatement, newThrow);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private async static Task<Document> MakeThrowAsInnerAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var throwStatement = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<ThrowStatementSyntax>().First();
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
            var newRoot = root.ReplaceNode(throwStatement, newThrow);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}