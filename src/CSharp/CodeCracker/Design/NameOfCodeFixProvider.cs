using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Text;
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
            {
                var parameter = FindParameterThatStringLiteralRefers(root, diagnosticSpan.Start, stringLiteral);
                if (parameter != null)
                {
                    var nameofArgument = parameter.Identifier.Text;
                    context.RegisterCodeFix(CodeAction.Create("Use nameof()", c => MakeNameOfAsync(context.Document, nameofArgument, stringLiteral, c)), diagnostic);
                }
            }
        }

        private async Task<Document> MakeNameOfAsync(Document document, string nameofArgument, LiteralExpressionSyntax stringLiteral, CancellationToken cancelationToken)
        {
            var newNameof = SyntaxFactory.ParseExpression($"nameof({nameofArgument})")
                                    .WithLeadingTrivia(stringLiteral.GetLeadingTrivia())
                                    .WithTrailingTrivia(stringLiteral.GetTrailingTrivia())
                                    .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync(cancelationToken);
            var newRoot = root.ReplaceNode(stringLiteral, newNameof);
            return document.WithSyntaxRoot(newRoot);
        }

        private ParameterSyntax FindParameterThatStringLiteralRefers(SyntaxNode root, int diagnosticSpanStart, LiteralExpressionSyntax stringLiteral)
        {
            var method = root.FindToken(diagnosticSpanStart).Parent.FirstAncestorOfType(typeof(ConstructorDeclarationSyntax), typeof(MethodDeclarationSyntax)) as BaseMethodDeclarationSyntax;

            return method?.ParameterList.Parameters.FirstOrDefault(m => m.Identifier.Value.ToString() == stringLiteral.Token.Value.ToString());
        }
    }
}