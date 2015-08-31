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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TernaryOperatorWithReturnCodeFixProvider)), Shared]
    public class TernaryOperatorWithReturnCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Return.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Change to ternary operator", c => MakeTernaryAsync(context.Document, diagnostic, c), nameof(TernaryOperatorWithReturnCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> MakeTernaryAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var ifStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
            var statementInsideIf = (ReturnStatementSyntax)(ifStatement.Statement is BlockSyntax ? ((BlockSyntax)ifStatement.Statement).Statements.Single() : ifStatement.Statement);
            var elseStatement = ifStatement.Else;
            var statementInsideElse = (ReturnStatementSyntax)(elseStatement.Statement is BlockSyntax ? ((BlockSyntax)elseStatement.Statement).Statements.Single() : elseStatement.Statement);
            var nullableSafeIfExpression = MakeStatementSafeForNullableReturnTypesAsync(document, statementInsideIf, cancellationToken);
            var nullableSafeElseExpression = MakeStatementSafeForNullableReturnTypesAsync(document, statementInsideElse, cancellationToken);
            Task.WaitAll(nullableSafeIfExpression, nullableSafeElseExpression);
            var ternary = SyntaxFactory.ParseStatement($"return {ifStatement.Condition.ToString()} ? {nullableSafeIfExpression.Result} : {nullableSafeElseExpression.Result};")
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(ifStatement, ternary);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static async Task<string> MakeStatementSafeForNullableReturnTypesAsync(Document document, ReturnStatementSyntax statement, CancellationToken cancellationToken)
        {
            var safeStatement = statement.Expression.ToString();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var returnType = semanticModel.GetTypeInfo(statement.Expression, cancellationToken);
            if (returnType.ConvertedType.IsValueType && returnType.ConvertedType.Name == "Nullable")
            {
                if (returnType.Type == null || returnType.Type.IsReferenceType)
                {
                    safeStatement = $"({returnType.ConvertedType.ToString()}){safeStatement}";
                }
            }

            return safeStatement;
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TernaryOperatorWithAssignmentCodeFixProvider)), Shared]
    public class TernaryOperatorWithAssignmentCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Change to ternary operator", c => MakeTernaryAsync(context.Document, diagnostic, c), nameof(TernaryOperatorWithAssignmentCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> MakeTernaryAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var ifStatement = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
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
            var newRoot = root.ReplaceNode(ifStatement, ternary);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}