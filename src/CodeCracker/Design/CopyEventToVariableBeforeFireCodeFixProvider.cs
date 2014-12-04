﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Design
{
    [ExportCodeFixProvider("CodeCrackerCopyEventToVariableBeforeFireCodeFixProvider", LanguageNames.CSharp)]
    public class CopyEventToVariableBeforeFireCodeFixProvider : CodeFixProvider
    {
        private const string SyntaxAnnotatinKind = "CC-CopyEvent";

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(CopyEventToVariableBeforeFireAnalyzer.DiagnosticId);
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
            var handlerName = "handler";

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

            var root = await document.GetSyntaxRootAsync(ct);

            var newRoot = root.ReplaceNode(invocation.Parent, invocation.Parent.WithAdditionalAnnotations(new SyntaxAnnotation(SyntaxAnnotatinKind)));
            newRoot = newRoot.InsertNodesAfter(GetMark(newRoot), new[] { variable as SyntaxNode, newInvocation as SyntaxNode });
            newRoot = newRoot.RemoveNode(GetMark(newRoot), SyntaxRemoveOptions.KeepNoTrivia);

            return document.WithSyntaxRoot(newRoot.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private static SyntaxNode GetMark(SyntaxNode node)
        {
            return node.DescendantNodes().First(n => n.GetAnnotations(SyntaxAnnotatinKind).Any());
        }
    }
}