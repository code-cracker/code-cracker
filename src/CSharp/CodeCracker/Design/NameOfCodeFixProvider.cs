using CodeCracker.Properties;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(NameOfCodeFixProvider)), Shared]
    public class NameOfCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.NameOf.ToDiagnosticId(), DiagnosticId.NameOf_External.ToDiagnosticId());

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(Resources.NameOfCodeFixProvider_Title, c => MakeNameOfAsync(context.Document, diagnostic, c), nameof(NameOfCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> MakeNameOfAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var stringLiteral = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();
            var nameofArgument = await GetIdentifierAsync(document, stringLiteral, root, diagnostic, cancellationToken).ConfigureAwait(false);
            var newNameof = SyntaxFactory.ParseExpression($"nameof({nameofArgument})")
                                    .WithLeadingTrivia(stringLiteral.GetLeadingTrivia())
                                    .WithTrailingTrivia(stringLiteral.GetTrailingTrivia())
                                    .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(stringLiteral, newNameof);
            return document.WithSyntaxRoot(newRoot);
        }

        private static async Task<string> GetIdentifierAsync(Document document, LiteralExpressionSyntax stringLiteral, SyntaxNode root, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var parameter = FindParameterThatStringLiteralRefers(root, diagnosticSpan.Start, stringLiteral);
            if (parameter != null)
                return parameter.Identifier.Text;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var symbol = semanticModel.LookupSymbols(stringLiteral.Token.SpanStart, null, stringLiteral.Token.ValueText).First();
            return symbol.ToDisplayParts().Last(AnalyzerExtensions.IsName).ToString();
        }

        private static ParameterSyntax FindParameterThatStringLiteralRefers(SyntaxNode root, int diagnosticSpanStart, LiteralExpressionSyntax stringLiteral)
        {
            var method = root.FindToken(diagnosticSpanStart).Parent.FirstAncestorOfType(typeof(ConstructorDeclarationSyntax), typeof(MethodDeclarationSyntax)) as BaseMethodDeclarationSyntax;
            return method?.ParameterList.Parameters.FirstOrDefault(m => m.Identifier.Value.ToString() == stringLiteral.Token.Value.ToString());
        }
    }
}