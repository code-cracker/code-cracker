﻿using Microsoft.CodeAnalysis;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TernaryOperatorWithReturnCodeFixProvider)), Shared]
    public class TernaryOperatorWithReturnCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Return.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
            context.RegisterCodeFix(CodeAction.Create("Change to ternary operator", c => MakeTernaryAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> MakeTernaryAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var statementInsideIf = (ReturnStatementSyntax)(ifStatement.Statement is BlockSyntax ? ((BlockSyntax)ifStatement.Statement).Statements.Single() : ifStatement.Statement);
            var elseStatement = ifStatement.Else;
            var statementInsideElse = (ReturnStatementSyntax)(elseStatement.Statement is BlockSyntax ? ((BlockSyntax)elseStatement.Statement).Statements.Single() : elseStatement.Statement);
            var ternary = SyntaxFactory.ParseStatement($"return {ifStatement.Condition.ToString()} ? {statementInsideIf.Expression.ToString()} : {statementInsideElse.Expression.ToString()};")
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(ifStatement, ternary);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TernaryOperatorWithAssignmentCodeFixProvider)), Shared]
    public class TernaryOperatorWithAssignmentCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
            context.RegisterCodeFix(CodeAction.Create("Change to ternary operator", c => MakeTernaryAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> MakeTernaryAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var expressionInsideIf = (ExpressionStatementSyntax)(ifStatement.Statement is BlockSyntax ? ((BlockSyntax)ifStatement.Statement).Statements.Single() : ifStatement.Statement);
            var elseStatement = ifStatement.Else;
            var expressionInsideElse = (ExpressionStatementSyntax)(elseStatement.Statement is BlockSyntax ? ((BlockSyntax)elseStatement.Statement).Statements.Single() : elseStatement.Statement);

            var assignmentExpressionInsideIf = (AssignmentExpressionSyntax)expressionInsideIf.Expression;
            var assignmentExpressionInsideElse = (AssignmentExpressionSyntax)expressionInsideElse.Expression;
            var variableIdentifierInsideIf = assignmentExpressionInsideIf.Left as IdentifierNameSyntax;
            var ternary = SyntaxFactory.ParseStatement($"{variableIdentifierInsideIf.Identifier.Text} = {ifStatement.Condition.ToString()} ? {assignmentExpressionInsideIf.Right.ToString()} : {assignmentExpressionInsideElse.Right.ToString()};")
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(ifStatement, ternary);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}