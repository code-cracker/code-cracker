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

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(RemoveUnreachableCodeCodeFixProvider)), Shared]
    public class RemoveUnreachableCodeCodeFixProvider : CodeFixProvider
    {
        public const string Message = "Remove unreacheable code";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create("CS0162");

        public sealed override FixAllProvider GetFixAllProvider() => RemoveUnreachableCodeFixAllProvider.Instance;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(Message, ct => RemoveUnreachableCodeAsync(context.Document, diagnostic, ct)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> RemoveUnreachableCodeAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var newDoc = document.WithSyntaxRoot(RemoveUnreachableStatement(root, node));
            return newDoc;
        }

        public static SyntaxNode RemoveUnreachableStatement(SyntaxNode root, SyntaxNode node)
        {
            if (node.Parent.IsKind(SyntaxKind.IfStatement, SyntaxKind.WhileStatement))
                return root.ReplaceNode(node, SyntaxFactory.Block());
            if (node.Parent.IsKind(SyntaxKind.ElseClause))
                return root.RemoveNode(node.Parent, SyntaxRemoveOptions.KeepNoTrivia);
            var statement = node as StatementSyntax;//for, while, foreach, if, throw, var, etc
            if (statement != null)
                return root.RemoveNode(statement, SyntaxRemoveOptions.KeepNoTrivia);
            var localDeclaration = node.FirstAncestorOfType<LocalDeclarationStatementSyntax>();
            if (localDeclaration != null)
                return root.RemoveNode(localDeclaration, SyntaxRemoveOptions.KeepNoTrivia);
            var expression = GetExpression(node);
            if (expression.Parent.IsKind(SyntaxKind.ForStatement))
                return root.RemoveNode(expression, SyntaxRemoveOptions.KeepNoTrivia);
            var expressionStatement = expression.FirstAncestorOfType<ExpressionStatementSyntax>();
            if (expressionStatement.Parent.IsKind(SyntaxKind.IfStatement, SyntaxKind.WhileStatement))
                return root.ReplaceNode(expressionStatement, SyntaxFactory.Block());
            if (expressionStatement.Parent.IsKind(SyntaxKind.ElseClause))
                return root.RemoveNode(expressionStatement.Parent, SyntaxRemoveOptions.KeepNoTrivia);
            return root.RemoveNode(expressionStatement, SyntaxRemoveOptions.KeepNoTrivia);
        }

        private static ExpressionSyntax GetExpression(SyntaxNode node)
        {
            var expression = node.Parent as ExpressionSyntax;
            var memberAccess = node.Parent as MemberAccessExpressionSyntax;
            while (memberAccess != null)
            {
                var parentMemberAccess = memberAccess.Parent as MemberAccessExpressionSyntax;
                if (parentMemberAccess != null)
                {
                    memberAccess = parentMemberAccess;
                }
                else
                {
                    expression = memberAccess.Parent as ExpressionSyntax;
                    break;
                }
            }
            return expression;
        }
    }
}