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

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerExistenceOperatorCodeFixProvider ", LanguageNames.CSharp), Shared]
    public class ExistenceOperatorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(ExistenceOperatorAnalyzer.DiagnosticId);
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
            var statement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().FirstOrDefault();
            if (statement != null)
                context.RegisterFix(CodeAction.Create("Use the existence operator", c => UseExistenceOperatorAsync(context.Document, statement, c)), diagnostic);
        }

        private async Task<Document> UseExistenceOperatorAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var statementInsideIf = ifStatement.Statement.GetSingleStatementFromPossibleBlock();
            var statementInsideElse = ifStatement.Else.Statement.GetSingleStatementFromPossibleBlock();
            var returnIf = statementInsideIf as ReturnStatementSyntax;
            var returnElse = statementInsideElse as ReturnStatementSyntax;
            if (returnIf != null && returnElse != null)
                return await UseExistenceOperatorAsyncWithReturn(document, ifStatement, cancellationToken, returnIf, returnElse);
            return await UseExistenceOperatorAsyncWithAssignment(document, ifStatement, cancellationToken, (ExpressionStatementSyntax)statementInsideIf, (ExpressionStatementSyntax)statementInsideElse);
        }

        private async Task<Document> UseExistenceOperatorAsyncWithReturn(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken, ReturnStatementSyntax returnIf, ReturnStatementSyntax returnElse)
        {
            var newMemberAccess = ((MemberAccessExpressionSyntax)returnIf.Expression).ToConditionalAccessExpression();
            var newReturn = SyntaxFactory.ReturnStatement(newMemberAccess)
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode<SyntaxNode, StatementSyntax>(ifStatement, newReturn);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private async Task<Document> UseExistenceOperatorAsyncWithAssignment(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken, ExpressionStatementSyntax expressionIf, ExpressionStatementSyntax expressionElse)
        {
            var memberAccessAssignment = (AssignmentExpressionSyntax)expressionIf.Expression;
            var newMemberAccess = ((MemberAccessExpressionSyntax)memberAccessAssignment.Right).ToConditionalAccessExpression();
            var newExpressionStatement = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, memberAccessAssignment.Left, newMemberAccess))
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode<SyntaxNode, StatementSyntax>(ifStatement, newExpressionStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}