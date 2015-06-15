using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Composition;
using System.Threading;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, nameof(StringRepresentationCodeFixProvider)), Shared]
    public class StringRepresentationCodeFixProvider : CodeFixProvider
    {
        public const string Id = nameof(StringRepresentationCodeFixProvider);
        public const string ToRegularId = Id + "ToRegularString";
        public const string ToVerbatimId = Id + "ToVerbatimString";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.StringRepresentation_RegularString.ToDiagnosticId(), DiagnosticId.StringRepresentation_VerbatimString.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var truncated = diagnostic.Properties["truncatedString"];
            var isVerbatimString = diagnostic.Properties["isVerbatim"] == "1";
            var message = isVerbatimString ? $"Convert \"{truncated}\" to regular string" : $"Convert \"{truncated}\" to verbatim string";
            var equivalenceKey = isVerbatimString ? ToRegularId : ToVerbatimId;
            context.RegisterCodeFix(
                CodeAction.Create(message, ct => CreateChangedDocumentAsync(context.Document, diagnostic, isVerbatimString, ct), equivalenceKey), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> CreateChangedDocumentAsync(Document document, Diagnostic diagnostic, bool isVerbatim, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var literalExpression = (LiteralExpressionSyntax)root.FindNode(diagnostic.Location.SourceSpan, getInnermostNodeForTie: true);
            var replacement = isVerbatim ? ToStringLiteral(literalExpression) : ToVerbatimStringLiteral(literalExpression);
            var finalReplacement = replacement.WithSameTriviaAs(literalExpression);
            var newRoot = root.ReplaceNode(literalExpression, finalReplacement);
            return document.WithSyntaxRoot(newRoot);
        }

        private static ExpressionSyntax ToStringLiteral(LiteralExpressionSyntax expression)
        {
            var str = (string)expression.Token.Value;
            return LiteralExpression(SyntaxKind.StringLiteralExpression, Literal(str));
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
    }
}