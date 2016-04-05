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

namespace CodeCracker.CSharp.Refactoring
{

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(MergeNestedIfCodeFixProvider)), Shared]
    public class MergeNestedIfCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.MergeNestedIf.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Merge nested if statements", c => MergeIfsAsync(context.Document, diagnostic.Location, c), nameof(MergeNestedIfCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> MergeIfsAsync(Document document, Location diagnosticLocation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var ifStatement = (IfStatementSyntax)root.FindNode(diagnosticLocation.SourceSpan);
            var newRoot = MergeIfs(ifStatement, root);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static SyntaxNode MergeIfs(IfStatementSyntax ifStatement, SyntaxNode root)
        {
            var nestedIf = (IfStatementSyntax)ifStatement.Statement.GetSingleStatementFromPossibleBlock();
            var nestedCondition = nestedIf.Condition;
            if (nestedCondition.IsAnyKind(SyntaxKind.LogicalOrExpression, SyntaxKind.ConditionalExpression, SyntaxKind.CoalesceExpression))
                nestedCondition = SyntaxFactory.ParenthesizedExpression(nestedCondition);
            var newIf = ifStatement
                .WithCondition(SyntaxFactory.BinaryExpression(SyntaxKind.LogicalAndExpression, ifStatement.Condition, nestedCondition))
                .WithStatement(nestedIf.Statement)
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia().AddRange(nestedIf.GetLeadingTrivia()))
                .WithAdditionalAnnotations(Formatter.Annotation);
            if (ifStatement.HasTrailingTrivia && nestedIf.HasTrailingTrivia && !ifStatement.GetTrailingTrivia().Equals(nestedIf.GetTrailingTrivia()))
                newIf = newIf.WithTrailingTrivia(ifStatement.GetTrailingTrivia().AddRange(nestedIf.GetTrailingTrivia()));
            var newRoot = root.ReplaceNode(ifStatement, newIf);
            return newRoot;
        }
    }
}