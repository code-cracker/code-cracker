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

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SplitIntoNestedIfCodeFixProvider)), Shared]
    public class SplitIntoNestedIfCodeFixProvider : CodeFixProvider
    {
        internal const string MessageFormat = "Split into nested ifs";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.SplitIntoNestedIf.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => SplitIntoNestedIfFixAllProvider.Instance;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(MessageFormat, c => CreateNestedIfAsync(context.Document, diagnostic.Location, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> CreateNestedIfAsync(Document document, Location diagnosticLocation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var condition = (BinaryExpressionSyntax)root.FindNode(diagnosticLocation.SourceSpan);
            var newRoot = CreateNestedIf(condition, root);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        internal static SyntaxNode CreateNestedIf(BinaryExpressionSyntax condition, SyntaxNode root)
        {
            var ifStatement = condition.FirstAncestorOfType<IfStatementSyntax>();
            var nestedIf = SyntaxFactory.IfStatement(condition.Right, ifStatement.Statement);
            var newStatement = ifStatement.Statement.IsKind(SyntaxKind.Block) ? (StatementSyntax)SyntaxFactory.Block(nestedIf) : nestedIf;
            var newIf = ifStatement.WithCondition(condition.Left)
                .WithStatement(newStatement)
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(ifStatement, newIf);
            return newRoot;
        }
    }
}