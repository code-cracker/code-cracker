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
    [ExportCodeFixProvider("CodeCrackerIfReturnTrueCodeFixProvider ", LanguageNames.CSharp), Shared]
    public class IfReturnTrueCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(IfReturnTrueAnalyzer.DiagnosticId);
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
                context.RegisterFix(CodeAction.Create("Return directly", c => ReturnConditionDirectlyAsync(context.Document, statement, c)), diagnostic);
        }

        private async Task<Document> ReturnConditionDirectlyAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var statementInsideIf = IfReturnTrueAnalyzer.GetSingleStatementFromPossibleBlock(ifStatement.Statement);
            var statementInsideElse = IfReturnTrueAnalyzer.GetSingleStatementFromPossibleBlock(ifStatement.Else.Statement);
            var returnIf = statementInsideIf as ReturnStatementSyntax;
            var returnElse = statementInsideElse as ReturnStatementSyntax;
            var condition = returnIf.Expression is LiteralExpressionSyntax
                && returnIf.Expression.IsKind(SyntaxKind.TrueLiteralExpression)
                && returnElse.Expression is LiteralExpressionSyntax
                && returnElse.Expression.IsKind(SyntaxKind.FalseLiteralExpression)
                ? ifStatement.Condition
                : SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression, ifStatement.Condition, SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
            var newReturn = SyntaxFactory.ReturnStatement(condition)
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode<SyntaxNode, StatementSyntax>(ifStatement, newReturn);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}