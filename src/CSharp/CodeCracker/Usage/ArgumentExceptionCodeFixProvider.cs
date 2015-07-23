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

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ArgumentExceptionCodeFixProvider)), Shared]
    public class ArgumentExceptionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ArgumentException.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var parameters = diagnostic.Properties.Where(p => p.Key.StartsWith("param"));
            foreach (var param in parameters)
            {
                var message = "Use '" + param.Value + "'";
                context.RegisterCodeFix(CodeAction.Create(message, c => FixParamAsync(context.Document, diagnostic, param.Value, c), nameof(ArgumentExceptionCodeFixProvider)), diagnostic);
            }
            return Task.FromResult(0);
        }

        private async static Task<Document> FixParamAsync(Document document, Diagnostic diagnostic, string newParamName, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var objectCreation = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var type = objectCreation.Type;
            var typeSymbol = semanticModel.GetSymbolInfo(type).Symbol as ITypeSymbol;
            var argumentList = objectCreation.ArgumentList as ArgumentListSyntax;
            var paramNameLiteral = argumentList.Arguments[1].Expression as LiteralExpressionSyntax;
            var paramNameOpt = semanticModel.GetConstantValue(paramNameLiteral);
            var currentParamName = paramNameOpt.Value as string;
            var newLiteral = SyntaxFactory.ParseExpression($"\"{newParamName}\"");
            var newRoot = root.ReplaceNode(paramNameLiteral, newLiteral);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}