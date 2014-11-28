using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerDirectEventInvocationCodeFixProvider", LanguageNames.CSharp)]
    public class DirectEventInvocationCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(DirectEventInvocationAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var sourceSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            context.RegisterFix(
                CodeAction.Create("Copy event reference to a variable", ct => CreateVariable(context.Document, invocation, ct)), diagnostic);
        }

        private async Task<Document> CreateVariable(Document document, InvocationExpressionSyntax invocation, CancellationToken ct)
        {
            var handlerName = "handler"; //TODO: validate variable name in the scope

            var variable =
                SyntaxFactory.LocalDeclarationStatement(
                    SyntaxFactory.VariableDeclaration(
                        SyntaxFactory.ParseTypeName("var"),
                        SyntaxFactory.SeparatedList(
                            new[]
                            {
                                SyntaxFactory.VariableDeclarator(
                                    SyntaxFactory.Identifier(handlerName),
                                    null,
                                    SyntaxFactory.EqualsValueClause(invocation.Expression))
                            })));

            var newInvocation =
                SyntaxFactory.IfStatement(
                    SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression,
                        SyntaxFactory.IdentifierName(handlerName),
                        SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.IdentifierName(handlerName),
                            invocation.ArgumentList)))
                .WithTrailingTrivia(invocation.Parent.GetTrailingTrivia());

            var originalBlock = invocation.FirstAncestorOrSelf<BlockSyntax>();
            var block = originalBlock.ReplaceNode(invocation, invocation.WithAdditionalAnnotations(new SyntaxAnnotation("exp")));
            var mark = GetMark(block);

            block = block.InsertNodesAfter(mark, new[] { variable as SyntaxNode, newInvocation as SyntaxNode });
            mark = GetMark(block);
            block = block.RemoveNode(mark, SyntaxRemoveOptions.KeepLeadingTrivia);

            return document
                        .WithSyntaxRoot((await document.GetSyntaxRootAsync(ct))
                        .ReplaceNode(originalBlock, block.WithLeadingTrivia().WithAdditionalAnnotations(Formatter.Annotation)));
        }

        private static SyntaxNode GetMark(BlockSyntax block)
        {
            return block.DescendantNodes().First(n => n.ContainsAnnotations);
        }
    }
}