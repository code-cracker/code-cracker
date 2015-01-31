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

namespace CodeCracker.Style
{
    [ExportCodeFixProvider("CodeCrackerStringFormatCodeFixProvider ", LanguageNames.CSharp), Shared]
    public class StringFormatCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.StringFormat.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            context.RegisterFix(CodeAction.Create("Change to string interpolation", c => MakeStringInterpolationAsync(context.Document, invocation, c)), diagnostic);
        }

        private async Task<Document> MakeStringInterpolationAsync(Document document, InvocationExpressionSyntax invocationExpression, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var memberSymbol = semanticModel.GetSymbolInfo(invocationExpression.Expression).Symbol;
            var argumentList = invocationExpression.ArgumentList;
            var arguments = argumentList.Arguments;
            var formatLiteral = (LiteralExpressionSyntax)arguments[0].Expression;
            var analyzingInterpolation = (InterpolatedStringSyntax)SyntaxFactory.ParseExpression($"${formatLiteral.Token.Text}");
            var interpolationArgs = arguments.Skip(1).ToArray();
            var expressionsToReplace = new Dictionary<ExpressionSyntax, ExpressionSyntax>();
            for (int i = 0; i < analyzingInterpolation.InterpolatedInserts.Count; i++)
            {
                var insert = analyzingInterpolation.InterpolatedInserts[i];
                expressionsToReplace.Add(insert.Expression, interpolationArgs[i].Expression);
            }
            var newStringInterpolation = analyzingInterpolation.ReplaceNodes(expressionsToReplace.Keys, (o, _) => expressionsToReplace[o]);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(invocationExpression, newStringInterpolation);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}