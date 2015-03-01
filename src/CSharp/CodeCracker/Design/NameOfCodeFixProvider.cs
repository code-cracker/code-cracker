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

namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider("CodeCrackerRethrowExceptionCodeFixProvider", LanguageNames.CSharp), Shared]
    public class NameOfCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.NameOf.ToDiagnosticId());

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var stringLiteral = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();
            if (stringLiteral != null)
                context.RegisterCodeFix(CodeAction.Create("Use nameof()", c => MakeNameOfAsync(context.Document, stringLiteral, c)), diagnostic);
        }

        private async Task<Document> MakeNameOfAsync(Document document, LiteralExpressionSyntax stringLiteral, CancellationToken cancelationToken)
        {
            var newNameof = SyntaxFactory.ParseExpression($"nameof({stringLiteral.Token.ValueText})")
                                    .WithLeadingTrivia(stringLiteral.GetLeadingTrivia())
                                    .WithTrailingTrivia(stringLiteral.GetTrailingTrivia())
                                    .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync(cancelationToken);
            var newRoot = root.ReplaceNode(stringLiteral, newNameof);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}