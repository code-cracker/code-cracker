using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Composition;

namespace CodeCracker.Style
{
    [ExportCodeFixProvider(Id, LanguageNames.CSharp), Shared]
    public class StringRepresentationCodeFixProvider : CodeFixProvider
    {
        public const string Id = "CodeCrackerStringRepresentationCodeFixProvider";
        public const string ToRegularId = Id + "ToRegularString";
        public const string ToVerbatimId = Id + "ToVerbatimString";

        static readonly ImmutableArray<string> fixableDiagnosticIds =
            ImmutableArray.Create(
                StringRepresentationAnalyzer.RegularStringId,
                StringRepresentationAnalyzer.VerbatimStringId);

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() => fixableDiagnosticIds;
        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken);

            foreach (var diagnostic in context.Diagnostics)
            {
                var literalExpression = root.FindNode(diagnostic.Location.SourceSpan,
                    getInnermostNodeForTie: true) as LiteralExpressionSyntax;

                if (literalExpression == null) continue;

                var isVerbatim = literalExpression.Token.Text.Length > 0
                    && literalExpression.Token.Text.StartsWith("@\"");

                Func<SyntaxNode, Task<Document>> createChangedDocument =
                    replacement =>
                    {
                        var finalReplacement = replacement.WithSameTriviaAs(literalExpression);
                        var newRoot = root.ReplaceNode(literalExpression, finalReplacement);
                        return Task.FromResult(context.Document.WithSyntaxRoot(newRoot));
                    };

                var truncatedString = Truncate((string)literalExpression.Token.Value, 20);

                var codeAction = isVerbatim
                    ? CodeAction.Create(
                        $"Convert \"{truncatedString}\" to regular string",
                        ct => createChangedDocument(ToStringLiteral(literalExpression)),
                        ToRegularId)
                    : CodeAction.Create(
                        $"Convert \"{truncatedString}\" to verbatim string",
                        ct => createChangedDocument(ToVerbatimStringLiteral(literalExpression)),
                        ToVerbatimId);

                context.RegisterFix(codeAction, diagnostic);
            }
        }

        private static string Truncate(string text, int length)
        {
            var normalized = new string(text.Cast<char>().Where(c => !char.IsControl(c)).ToArray());
            return normalized.Length <= length
                ? normalized
                : normalized.Substring(0, length - 1) + "\u2026";
        }

        private static string StringToVerbatimText(string s)
        {
            var builder = new StringBuilder(s.Length + 3);
            builder.Append("@\"");
            foreach (var c in s)
            {
                if (c == '"')
                    builder.Append("\"\"");
                else
                    builder.Append(c);
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