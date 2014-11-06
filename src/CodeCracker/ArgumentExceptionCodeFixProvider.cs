using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;

namespace CodeCracker
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

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type declaration identified by the diagnostic.
            var objectCreation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().First();

            var ancestorMethod = objectCreation.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            var parameters = ancestorMethod.ParameterList.Parameters.Select(p => p.Identifier.ToString()).ToArray();

            // Register a code action that will invoke the fix.
            foreach (var param in parameters)
            {
                string message = "Use '" + param + "'";
                context.RegisterFix(
                    CodeAction.Create(message, c => FixParamAsync(context.Document, objectCreation, param, c)),
                    diagnostic);
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