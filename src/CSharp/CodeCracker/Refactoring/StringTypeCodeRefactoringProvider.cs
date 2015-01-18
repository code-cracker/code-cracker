using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System;
using System.Text;
using System.Threading.Tasks;

namespace CodeCracker.Refactoring
{
    [ExportCodeRefactoringProvider(Id, LanguageNames.CSharp)]
    public class StringTypeCodeRefactoringProvider : CodeRefactoringProvider
    {
        public const string Id = "CodeCrackerStringTypeRefactoring";
        public const string ToRegularId = Id + "ToRegularString";
        public const string ToVerbatimId = Id + "ToVerbatimString";

        public override async Task ComputeRefactoringsAsync(CodeRefactoringContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);
            var node = root.FindNode(context.Span);

            if (node.CSharpKind() != SyntaxKind.StringLiteralExpression)
            {
                return;
            }

            var literalExpression = (LiteralExpressionSyntax)node;
            var isVerbatim = literalExpression.Token.Text.Length > 0
                && literalExpression.Token.Text.StartsWith("@\"");

            Func<SyntaxNode,Task<Document>> createChangedDocument =
                replacement => {
                    var finalReplacement = replacement.WithSameTriviaAs(node);
                    var newRoot = root.ReplaceNode(node, finalReplacement);
                    return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                };

            var codeAction = isVerbatim 
                ? CodeAction.Create("To regular string", ct => createChangedDocument(ToStringLiteral(literalExpression)),ToRegularId) 
                : CodeAction.Create("To verbatim string",ct => createChangedDocument(ToVerbatimStringLiteral(literalExpression)),ToVerbatimId);

            context.RegisterRefactoring(codeAction);
        }

        private static string StringToVerbatimText(string s)
        {
            var builder = new StringBuilder(s.Length + 3);
            builder.Append("@\"");
            foreach(var c in s)
            {
                if (c == '"')
                {
                    builder.Append("\"\"");
                }
                else
                {
                    builder.Append(c);
                }
            }
            builder.Append("\"");
            return builder.ToString();
        }

        private static ExpressionSyntax ToVerbatimStringLiteral(LiteralExpressionSyntax expression)
        {
            var str = (string)expression.Token.Value;
            return LiteralExpression(SyntaxKind.StringLiteralExpression,
                Literal(
                    TriviaList(),
                    StringToVerbatimText(str),
                    str,
                    TriviaList()));
        }

        private static ExpressionSyntax ToStringLiteral(LiteralExpressionSyntax expression)
        {
            var str = (string)expression.Token.Value;
            return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(str));
        }
    }
}
