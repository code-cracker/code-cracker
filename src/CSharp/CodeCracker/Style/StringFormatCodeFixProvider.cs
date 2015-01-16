using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
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
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(StringFormatAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            context.RegisterFix(CodeAction.Create("Change to string interpolation", c => MakeStringInterpolationAsync(context.Document, invocation, c)), diagnostic);
        }

        private async Task<Document> MakeStringInterpolationAsync(Document document, InvocationExpressionSyntax invocationExpression , CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync();
            var memberExpression = invocationExpression.Expression as MemberAccessExpressionSyntax;
            var memberSymbol = semanticModel.GetSymbolInfo(memberExpression).Symbol;
            var argumentList = invocationExpression.ArgumentList as ArgumentListSyntax;
            var arguments = argumentList.Arguments;
            var formatLiteral = (LiteralExpressionSyntax)arguments[0].Expression;
            var format = (string)semanticModel.GetConstantValue(formatLiteral).Value;
            var escapedFormat = format.Replace("\n", @"\n").Replace("\r", @"\r").Replace("\f", @"\f").Replace("\"","\\\"");
            var newParams = new List<object>();
            foreach (var param in arguments.Skip(1))
            {
                newParams.Add("{" + param.Expression.ToString() + "}");
            }
            var interpolatedStringText = "$\"" + string.Format(escapedFormat, newParams.ToArray()) + "\"";
            var newStringInterpolation = SyntaxFactory.ParseExpression(interpolatedStringText)
                .WithSameTriviaAs(invocationExpression)
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(invocationExpression, newStringInterpolation);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}