using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeCracker.CSharp.Refactoring
{

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ChangeAnyToAllCodeFixProvider)), Shared]
    public class ChangeAnyToAllCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ChangeAnyToAll.ToDiagnosticId(), DiagnosticId.ChangeAllToAny.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var message = diagnostic.Id == DiagnosticId.ChangeAnyToAll.ToDiagnosticId() ? "Change Any to All" : "Change All To Any";
            context.RegisterCodeFix(CodeAction.Create(message, c => ConvertAsync(context.Document, diagnostic.Location, c), nameof(ChangeAnyToAllCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> ConvertAsync(Document document, Location diagnosticLocation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var invocation = root.FindNode(diagnosticLocation.SourceSpan).FirstAncestorOfType<InvocationExpressionSyntax>();
            var newInvocation = CreateNewInvocation(invocation)
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = ReplaceInvocation(invocation, newInvocation, root);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static SyntaxNode ReplaceInvocation(InvocationExpressionSyntax invocation, ExpressionSyntax newInvocation, SyntaxNode root)
        {
            ExpressionSyntax lastExpression = invocation;
            while (lastExpression.Parent.IsAnyKind(SyntaxKind.MemberBindingExpression, SyntaxKind.SimpleMemberAccessExpression,
                SyntaxKind.ConditionalAccessExpression, SyntaxKind.LogicalNotExpression))
                lastExpression = (ExpressionSyntax)lastExpression.Parent;
            var lastExpressionWithNewInvocation = lastExpression.ReplaceNode(invocation, newInvocation);
            if (lastExpression.IsKind(SyntaxKind.LogicalNotExpression))
                return root.ReplaceNode(lastExpression, ((PrefixUnaryExpressionSyntax)lastExpressionWithNewInvocation).Operand);
            var negatedLastExpression = SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, lastExpressionWithNewInvocation);
            var newRoot = root.ReplaceNode(lastExpression, negatedLastExpression);
            return newRoot;
        }

        internal static ExpressionSyntax CreateNewInvocation(InvocationExpressionSyntax invocation)
        {
            var methodName = ChangeAnyToAllAnalyzer.GetName(invocation).ToString();
            var nameToCheck = methodName == "Any" ? ChangeAnyToAllAnalyzer.allName : ChangeAnyToAllAnalyzer.anyName;
            var newInvocation = invocation.WithExpression(ChangeAnyToAllAnalyzer.CreateExpressionWithNewName(invocation, nameToCheck));
            var comparisonExpression = (ExpressionSyntax)((LambdaExpressionSyntax)newInvocation.ArgumentList.Arguments.First().Expression).Body;
            var newComparisonExpression = CreateNewComparison(comparisonExpression);
            newComparisonExpression = RemoveParenthesis(newComparisonExpression);
            newInvocation = newInvocation.ReplaceNode(comparisonExpression, newComparisonExpression);
            return newInvocation;
        }

        private static ExpressionSyntax CreateNewComparison(ExpressionSyntax comparisonExpression)
        {
            if (comparisonExpression.IsKind(SyntaxKind.ConditionalExpression))
                return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                    SyntaxFactory.ParenthesizedExpression(comparisonExpression),
                    SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
            if (comparisonExpression.IsKind(SyntaxKind.LogicalNotExpression))
                return ((PrefixUnaryExpressionSyntax)comparisonExpression).Operand;
            if (comparisonExpression.IsKind(SyntaxKind.EqualsExpression))
            {
                var comparisonBinary = (BinaryExpressionSyntax)comparisonExpression;
                if (comparisonBinary.Right.IsKind(SyntaxKind.TrueLiteralExpression))
                    return comparisonBinary.WithRight(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
                if (comparisonBinary.Left.IsKind(SyntaxKind.TrueLiteralExpression))
                    return comparisonBinary.WithLeft(SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
                if (comparisonBinary.Right.IsKind(SyntaxKind.FalseLiteralExpression))
                    return comparisonBinary.Left;
                if (comparisonBinary.Left.IsKind(SyntaxKind.FalseLiteralExpression))
                    return comparisonBinary.Right;
                return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.NotEqualsExpression);
            }
            if (comparisonExpression.IsKind(SyntaxKind.NotEqualsExpression))
            {
                var comparisonBinary = (BinaryExpressionSyntax)comparisonExpression;
                if (comparisonBinary.Right.IsKind(SyntaxKind.TrueLiteralExpression))
                    return comparisonBinary.Left;
                if (comparisonBinary.Left.IsKind(SyntaxKind.TrueLiteralExpression))
                    return comparisonBinary.Right;
                return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.EqualsExpression);
            }
            if (comparisonExpression.IsKind(SyntaxKind.GreaterThanExpression))
                return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.LessThanOrEqualExpression);
            if (comparisonExpression.IsKind(SyntaxKind.GreaterThanOrEqualExpression))
                return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.LessThanExpression);
            if (comparisonExpression.IsKind(SyntaxKind.LessThanExpression))
                return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.GreaterThanOrEqualExpression);
            if (comparisonExpression.IsKind(SyntaxKind.LessThanOrEqualExpression))
                return CreateNewBinaryExpression(comparisonExpression, SyntaxKind.GreaterThanExpression);
            if (comparisonExpression.IsKind(SyntaxKind.TrueLiteralExpression))
                return SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression);
            if (comparisonExpression.IsKind(SyntaxKind.FalseLiteralExpression))
                return SyntaxFactory.LiteralExpression(SyntaxKind.TrueLiteralExpression);
            return SyntaxFactory.BinaryExpression(SyntaxKind.EqualsExpression,
                comparisonExpression,
                SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression));
        }

        private static ExpressionSyntax RemoveParenthesis(ExpressionSyntax expression) =>
            expression.IsKind(SyntaxKind.ParenthesizedExpression) ? ((ParenthesizedExpressionSyntax)expression).Expression : expression;

        private static BinaryExpressionSyntax CreateNewBinaryExpression(ExpressionSyntax comparisonExpression, SyntaxKind kind)
        {
            var comparisonBinary = (BinaryExpressionSyntax)comparisonExpression;
            var left = comparisonBinary.Left;
            var newComparison = SyntaxFactory.BinaryExpression(kind,
                left.IsKind(SyntaxKind.ConditionalExpression) ? SyntaxFactory.ParenthesizedExpression(left) : left,
                comparisonBinary.Right);
            return newComparison;
        }
    }
}