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
using System;

namespace CodeCracker.Refactoring
{
    [ExportCodeFixProvider("CodeCrackerInvertForCodeFixProvider", LanguageNames.CSharp), Shared]
    public class InvertForCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(InvertForAnalyzer.DiagnosticId);
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
            var @for = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ForStatementSyntax>().First();
            context.RegisterFix(CodeAction.Create("Invert For Loop.", c => InvertForAsync(context.Document, @for, c)), diagnostic);
            //context.RegisterFix(CodeAction.Create("Throw original exception", c => MakeThrowAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> InvertForAsync(Document document, ForStatementSyntax @for, CancellationToken c)
        {
            if (InvertForAnalyzer.IsPostIncrement(@for.Incrementors[0]))
            {
                return await ConvertToDecrementingCounterForLoop(document, @for);
            }
            return await ConvertToIncrementingCounterForLoop(document, @for);
        }

        static readonly LiteralExpressionSyntax One = SyntaxFactory.LiteralExpression(SyntaxKind.NumericLiteralExpression, SyntaxFactory.Literal(
                            SyntaxFactory.TriviaList(),
                            @"1", 1,
                            SyntaxFactory.TriviaList())
                            );

        private static async Task<Document> ConvertToIncrementingCounterForLoop(Document document, ForStatementSyntax @for)
        {
            var condition = (BinaryExpressionSyntax)@for.Condition;
            var newEndValue = @for.Declaration != null
                ? ((BinaryExpressionSyntax)@for.Declaration.Variables[0].Initializer.Value).Left
                : ((BinaryExpressionSyntax)(@for.Initializers[0] as AssignmentExpressionSyntax).Right).Left;

            var newStartValue = ((BinaryExpressionSyntax)@for.Condition).Right;

            var newDeclaration = ReplaceStartValue(@for.Declaration, newStartValue);
            var newInitializers = ReplaceStartValue(@for.Initializers, newStartValue);

            var newCondition = condition
                .WithOperatorToken(SyntaxFactory.Token(SyntaxKind.LessThanToken))
                .WithRight(newEndValue);

            return await ReplaceFor(document, @for, 
                newDeclaration, 
                newInitializers,
                newCondition);
        }

        private static async Task<Document> ConvertToDecrementingCounterForLoop(Document document, ForStatementSyntax @for)
        {
            var condition = (BinaryExpressionSyntax)@for.Condition;

            var newEndValue = @for.Declaration != null 
                ? @for.Declaration.Variables[0].Initializer.Value 
                : (@for.Initializers[0] as AssignmentExpressionSyntax).Right;


            var newStartValue = SyntaxFactory.BinaryExpression(SyntaxKind.SubtractExpression,
                condition.Right, One);

            var newDeclaration = ReplaceStartValue(@for.Declaration, newStartValue); 
            var newInitializers = ReplaceStartValue(@for.Initializers, newStartValue);

            var newCondition = condition
                .WithOperatorToken(SyntaxFactory.Token(SyntaxKind.GreaterThanEqualsToken))
                .WithRight(newEndValue);
            
            return await ReplaceFor(document, @for, 
                newDeclaration, 
                newInitializers,
                newCondition);
        }

        static SeparatedSyntaxList<ExpressionSyntax> ReplaceStartValue(SeparatedSyntaxList<ExpressionSyntax> initializers, ExpressionSyntax newStartValue)
        {
            if (initializers.Count != 1)
            {
                return initializers;
            }

            var newInitializer = (initializers[0] as AssignmentExpressionSyntax)
                .WithRight(newStartValue);

            return new SeparatedSyntaxList<ExpressionSyntax>().Add(newInitializer);
        }

        static VariableDeclarationSyntax ReplaceStartValue(VariableDeclarationSyntax declaration, ExpressionSyntax newStartValue)
        {
            if (declaration == null) return null;

            var variable = declaration.Variables[0]
                .WithInitializer(SyntaxFactory.EqualsValueClause(newStartValue));

            return declaration
                .WithVariables(new SeparatedSyntaxList<VariableDeclaratorSyntax>().Add(variable));
        }

        static async Task<Document> ReplaceFor(Document document, ForStatementSyntax oldFor,
            VariableDeclarationSyntax newDeclaration,
            SeparatedSyntaxList<ExpressionSyntax> newInitializers,
            BinaryExpressionSyntax newCondition
            )
        {
            var newFor = oldFor
                .WithDeclaration(newDeclaration)
                .WithInitializers(newInitializers)
                .WithCondition(newCondition)
                .WithIncrementors(ToggleIncrement(oldFor))
                .WithAdditionalAnnotations(Formatter.Annotation);

            return await ReplaceFor(document, oldFor, newFor);
        }

        static SeparatedSyntaxList<ExpressionSyntax> ToggleIncrement(ForStatementSyntax @for)
        {
            var incrementor = (PostfixUnaryExpressionSyntax)@for.Incrementors[0];
            var newIncrementor = SyntaxFactory.PostfixUnaryExpression(
                InvertForAnalyzer.IsPostIncrement(incrementor) ? SyntaxKind.PostDecrementExpression : SyntaxKind.PostIncrementExpression,
                incrementor.Operand
                );

            return new SeparatedSyntaxList<ExpressionSyntax>().Add(newIncrementor);
        }

        static async Task<Document> ReplaceFor(Document document, ForStatementSyntax oldFor, ForStatementSyntax newFor)
        {
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(oldFor, newFor);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
