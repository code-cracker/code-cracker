using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ConvertToExpressionBodiedMemberCodeFixProvider)), Shared]
    public class ConvertToExpressionBodiedMemberCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ConvertToExpressionBodiedMember.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(
                "Convert to an expression bodied member.", c => ConvertToExpressionBodiedMemberAsync(context.Document, diagnostic, c), nameof(ConvertToExpressionBodiedMemberCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }
        private static async Task<Document> ConvertToExpressionBodiedMemberAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration != null)
            {
                return await ConvertToExpressionBodiedMemberAsync(document, methodDeclaration, cancellationToken);
            }
            else
            {
                var basePropertyDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BasePropertyDeclarationSyntax>().First();
                return await ConvertToExpressionBodiedMemberAsync(document, basePropertyDeclaration, cancellationToken);
            }
        }

        private static async Task<Document> ConvertToExpressionBodiedMemberAsync(Document document, BasePropertyDeclarationSyntax declaration, CancellationToken cancellationToken)
        {
            var accessors = declaration.AccessorList.Accessors;
            var body = accessors[0].Body;
            var returnStatement = body.Statements[0] as ReturnStatementSyntax;

            var arrowExpression = SyntaxFactory.ArrowExpressionClause(
                returnStatement.Expression);

            var newDeclaration = declaration;

            newDeclaration = ((dynamic)declaration)
                .WithAccessorList(null)
                .WithExpressionBody(arrowExpression)
                .WithSemicolon(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            newDeclaration = newDeclaration.WithAdditionalAnnotations(Formatter.Annotation);

            return await ReplaceNodeAsync(document, declaration, newDeclaration, cancellationToken);
        }

        public static async Task<Document> ReplaceNodeAsync(Document document, SyntaxNode @old, SyntaxNode @new, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(@old, @new);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static async Task<Document> ConvertToExpressionBodiedMemberAsync(
            Document document,
            BaseMethodDeclarationSyntax declaration,
            CancellationToken cancellationToken)
        {
            var body = declaration.Body;
            var returnStatement = body.Statements[0] as ReturnStatementSyntax;

            var arrowExpression = SyntaxFactory.ArrowExpressionClause(returnStatement.Expression);

            var newDeclaration = declaration;

            newDeclaration = ((dynamic)newDeclaration)
                .WithBody(null)
                .WithExpressionBody(arrowExpression)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            newDeclaration = newDeclaration.WithAdditionalAnnotations(Formatter.Annotation);

            return await ReplaceNodeAsync(document, declaration, newDeclaration, cancellationToken);
        }
    }
}