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

namespace CodeCracker.Usage
{
    [ExportCodeFixProvider("CodeCrackerArgumentExceptionCodeFixProvider", LanguageNames.CSharp), Shared]
    public class ArgumentExceptionCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(ArgumentExceptionAnalyzer.DiagnosticId);
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
            var objectCreation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();

            var accessor = objectCreation.FirstAncestorOrSelf<AccessorDeclarationSyntax>();
            if (accessor != null)
            {
                if (!accessor.IsKind(SyntaxKind.SetAccessorDeclaration)) return;
                var message = "Use 'value'";
                context.RegisterFix(CodeAction.Create(message, c => FixParamAsync(context.Document, objectCreation, "value", c)), diagnostic);
            }
            else
            {
                var parametersList = objectCreation.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>()?.ParameterList;
                var parameters = parametersList.Parameters.Select(p => p.Identifier.ToString()).ToArray();
                foreach (var param in parameters)
                {
                    var message = "Use '" + param + "'";
                    context.RegisterFix(CodeAction.Create(message, c => FixParamAsync(context.Document, objectCreation, param, c)), diagnostic);
                }
            }
        }

        private async Task<Document> FixParamAsync(Document document, ObjectCreationExpressionSyntax objectCreation, string newParamName, CancellationToken cancellationToken)
        {

            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var type = objectCreation.Type;
            var typeSymbol = semanticModel.GetSymbolInfo(type).Symbol as ITypeSymbol;
            var argumentList = objectCreation.ArgumentList as ArgumentListSyntax;
            var paramNameLiteral = argumentList.Arguments[1].Expression as LiteralExpressionSyntax;
            var paramNameOpt = semanticModel.GetConstantValue(paramNameLiteral);
            var currentParamName = paramNameOpt.Value as string;

            var newLiteral = SyntaxFactory.ParseExpression(string.Format("\"{0}\"", newParamName));

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(paramNameLiteral, newLiteral);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}