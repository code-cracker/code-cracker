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

namespace CodeCracker.CSharp.Refactoring
{

    [ExportCodeFixProvider("CodeCrackerNumericLiteralCodeFixProvider", LanguageNames.CSharp), Shared]
    public class NumericLiteralCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.NumericLiteral.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Swap decimal and hexadecimal literals", c => ChangeLiteralAsync(context.Document, diagnostic.Location, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> ChangeLiteralAsync(Document document, Location diagnosticLocation, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var literal = (LiteralExpressionSyntax)root.FindNode(diagnosticLocation.SourceSpan);
            var newRoot = CreateNewLiteral(literal, root);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static SyntaxNode CreateNewLiteral(LiteralExpressionSyntax literal, SyntaxNode root)
        {
            SyntaxNode newRoot;
            if (literal.Token.ValueText != literal.Token.Text)
            {
                var newTokenText = literal.Token.ValueText;
                var newLiteral = literal.WithToken(SyntaxFactory.ParseToken(newTokenText));
                newRoot = root.ReplaceNode(literal, newLiteral);
            }
            else
            {
                var value = (dynamic)literal.Token.Value;
                if (literal.Parent != null && literal.Parent.IsKind(SyntaxKind.UnaryMinusExpression))
                {
                    var newTokenText = (string)("0x" + (value * -1).ToString("X"));
                    var newLiteral = literal.WithToken(SyntaxFactory.ParseToken(newTokenText))
                        .WithLeadingTrivia(literal.Parent.GetLeadingTrivia())
                        .WithTrailingTrivia(literal.Parent.GetTrailingTrivia());
                    newRoot = root.ReplaceNode(literal.Parent, newLiteral);
                }
                else
                {
                    var newTokenText = (string)("0x" + value.ToString("X"));
                    var newLiteral = literal.WithToken(SyntaxFactory.ParseToken(newTokenText));
                    newRoot = root.ReplaceNode(literal, newLiteral);
                }
            }
            return newRoot;
        }
    }
}