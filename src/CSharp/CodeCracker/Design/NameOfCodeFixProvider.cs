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

namespace CodeCracker.Design
{
    [ExportCodeFixProvider("CodeCrackerRethrowExceptionCodeFixProvider", LanguageNames.CSharp), Shared]
    public class NameOfCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(NameOfAnalyzer.DiagnosticId);
        }
        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var stringLiteral = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LiteralExpressionSyntax>().FirstOrDefault();
            if (stringLiteral != null)
                context.RegisterFix(CodeAction.Create("Use nameof()", c => MakeNameOfAsync(context.Document, stringLiteral, c)), diagnostic);
        }

        private async Task<Document> MakeNameOfAsync(Document document, LiteralExpressionSyntax stringLiteral, CancellationToken cancelationToken)
        {
            var methodDeclaration = stringLiteral.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration != null)
            {
                var methodParameter = methodDeclaration.ParameterList.Parameters.First();

                return await NewDocument(document, stringLiteral, methodParameter);
            }
            else
            {
                var constructorDeclaration = stringLiteral.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
                var constructorParameter = constructorDeclaration.ParameterList.Parameters.First();

                return await NewDocument(document, stringLiteral, constructorParameter);
            }
        }

        private async Task<Document> NewDocument(Document document, LiteralExpressionSyntax stringLiteral, ParameterSyntax methodParameter)
        {
            var newNameof = SyntaxFactory.ParseExpression("nameof(\{methodParameter.Identifier.Value})")
                                    .WithLeadingTrivia(stringLiteral.GetLeadingTrivia())
                                    .WithTrailingTrivia(stringLiteral.GetTrailingTrivia())
                                    .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode<SyntaxNode, SyntaxNode>(stringLiteral, newNameof);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}