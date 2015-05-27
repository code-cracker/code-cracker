using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(StringFormatCodeFixProvider)), Shared]
    public class StringFormatCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.StringFormat.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            context.RegisterCodeFix(CodeAction.Create("Change to string interpolation", c => MakeStringInterpolationAsync(context.Document, invocation, c)), diagnostic);
        }

        private async Task<Document> MakeStringInterpolationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var memberSymbol = semanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol;
            var argumentList = invocationExpression.ArgumentList;
            var arguments = argumentList.Arguments;
            var formatLiteral = (LiteralExpressionSyntax)arguments[0].Expression;
            var analyzingInterpolation = (InterpolatedStringExpressionSyntax)SyntaxFactory.ParseExpression($"${formatLiteral.Token.Text}");
            var interpolationArgs = arguments.Skip(1).ToArray();
            var expressionsToReplace = new Dictionary<ExpressionSyntax, ExpressionSyntax>();
            foreach (var interpolation in analyzingInterpolation.Contents.OfType<InterpolationSyntax>())
            {
                var index = (int)((LiteralExpressionSyntax)interpolation.Expression).Token.Value;
                var expression = interpolationArgs[index].Expression;
                var conditional = expression as ConditionalExpressionSyntax;
                if (conditional != null) expression = SyntaxFactory.ParenthesizedExpression(expression);
                expressionsToReplace.Add(interpolation.Expression, expression);
            }
            var newStringInterpolation = analyzingInterpolation.ReplaceNodes(expressionsToReplace.Keys, (o, _) => expressionsToReplace[o]);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(invocationExpression, newStringInterpolation);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}