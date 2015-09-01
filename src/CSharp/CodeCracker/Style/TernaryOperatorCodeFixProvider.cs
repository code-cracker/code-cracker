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
    public abstract class TernaryOperatorCodeFixProviderBase : CodeFixProvider
    {
        protected static ExpressionSyntax MakeTernaryOperand(ExpressionSyntax expression, SemanticModel semanticModel, ITypeSymbol type, TypeSyntax typeSyntax)
        {
            if (type?.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
            {
                var constValue = semanticModel.GetConstantValue(expression);
                if (constValue.HasValue && constValue.Value == null)
                {
                    return SyntaxFactory.CastExpression(typeSyntax, expression);
                }
            }
            return expression;
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TernaryOperatorWithReturnCodeFixProvider)), Shared]
    public class TernaryOperatorWithReturnCodeFixProvider : TernaryOperatorCodeFixProviderBase
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

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var type = semanticModel.GetTypeInfo(statementInsideIf.Expression).ConvertedType;
            var typeSyntax = SyntaxFactory.IdentifierName(type.ToMinimalDisplayString(semanticModel, statementInsideIf.SpanStart));
            var trueExpression = MakeTernaryOperand(statementInsideIf.Expression, semanticModel, type, typeSyntax);
            var falseExpression = MakeTernaryOperand(statementInsideElse.Expression, semanticModel, type, typeSyntax);

            var ternary =
                SyntaxFactory.ReturnStatement(
                    SyntaxFactory.ConditionalExpression(ifStatement.Condition, trueExpression, falseExpression))
                    .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(ifStatement, ternary);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(TernaryOperatorWithAssignmentCodeFixProvider)), Shared]
    public class TernaryOperatorWithAssignmentCodeFixProvider : TernaryOperatorCodeFixProviderBase
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
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var type = semanticModel.GetTypeInfo(assignmentExpressionInsideIf).Type;
            var typeSyntax = SyntaxFactory.IdentifierName(type.ToMinimalDisplayString(semanticModel, assignmentExpressionInsideIf.SpanStart));
            var trueExpression = MakeTernaryOperand(assignmentExpressionInsideIf.Right, semanticModel, type, typeSyntax);
            var falseExpression = MakeTernaryOperand(assignmentExpressionInsideElse.Right, semanticModel, type, typeSyntax);
            var ternary =
                SyntaxFactory.ExpressionStatement(
                    SyntaxFactory.AssignmentExpression(
                        assignmentExpressionInsideIf.Kind(),
                        assignmentExpressionInsideIf.Left,
                        SyntaxFactory.ConditionalExpression(ifStatement.Condition, trueExpression, falseExpression)))
                    .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                    .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                    .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(ifStatement, ternary);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}