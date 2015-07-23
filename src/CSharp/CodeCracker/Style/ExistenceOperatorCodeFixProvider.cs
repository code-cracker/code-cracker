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

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExistenceOperatorCodeFixProvider)), Shared]
    public class ExistenceOperatorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ExistenceOperator.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Use the existence operator", c => UseExistenceOperatorAsync(context.Document, diagnostic, c), nameof(ExistenceOperatorCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> UseExistenceOperatorAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var ifStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().FirstOrDefault();
            var statementInsideIf = ifStatement.Statement.GetSingleStatementFromPossibleBlock();
            var statementInsideElse = ifStatement.Else.Statement.GetSingleStatementFromPossibleBlock();
            var returnIf = statementInsideIf as ReturnStatementSyntax;
            var returnElse = statementInsideElse as ReturnStatementSyntax;
            if (returnIf != null && returnElse != null)
                return await UseExistenceOperatorAsyncWithReturnAsync(document, ifStatement, cancellationToken, returnIf);
            return await UseExistenceOperatorAsyncWithAssignmentAsync(document, ifStatement, cancellationToken, (ExpressionStatementSyntax)statementInsideIf);
        }

        private static async Task<Document> UseExistenceOperatorAsyncWithReturnAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken, ReturnStatementSyntax returnIf)
        {
            var newMemberAccess = ((MemberAccessExpressionSyntax)returnIf.Expression).ToConditionalAccessExpression();
            var newReturn = SyntaxFactory.ReturnStatement(newMemberAccess)
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(ifStatement, newReturn);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static async Task<Document> UseExistenceOperatorAsyncWithAssignmentAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken, ExpressionStatementSyntax expressionIf)
        {
            var memberAccessAssignment = (AssignmentExpressionSyntax)expressionIf.Expression;
            var newMemberAccess = ((MemberAccessExpressionSyntax)memberAccessAssignment.Right).ToConditionalAccessExpression();
            var newExpressionStatement = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, memberAccessAssignment.Left, newMemberAccess))
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(ifStatement, newExpressionStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}