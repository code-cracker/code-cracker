﻿using Microsoft.CodeAnalysis;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(CopyEventToVariableBeforeFireCodeFixProvider)), Shared]
    public class CopyEventToVariableBeforeFireCodeFixProvider : CodeFixProvider
    {
        private const string SyntaxAnnotatinKind = "CC-CopyEvent";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.CopyEventToVariableBeforeFire.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var sourceSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();

            context.RegisterCodeFix(
                CodeAction.Create("Copy event reference to a variable", ct => CreateVariableAsync(context.Document, invocation, ct)), diagnostic);
        }

        private async Task<Document> CreateVariableAsync(Document document, InvocationExpressionSyntax invocation, CancellationToken ct)
        {
            const string handlerName = "handler";

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
                                        SyntaxFactory.EqualsValueClause(invocation.Expression.WithoutLeadingTrivia().WithoutTrailingTrivia()))
                                })))
                    .WithLeadingTrivia(invocation.Parent.GetLeadingTrivia());

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

            var oldNode = invocation.Parent;
            var newNode = invocation.Parent.WithAdditionalAnnotations(new SyntaxAnnotation(SyntaxAnnotatinKind));

            if (oldNode.Parent.IsEmbeddedStatementOwner())
                newNode = SyntaxFactory.Block((StatementSyntax)newNode);

            var newRoot = root.ReplaceNode(oldNode, newNode);
            newRoot = newRoot.InsertNodesAfter(GetMark(newRoot), new SyntaxNode[] { variable, newInvocation });
            newRoot = newRoot.RemoveNode(GetMark(newRoot), SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private static SyntaxNode GetMark(SyntaxNode node) =>
            node.DescendantNodes().First(n => n.GetAnnotations(SyntaxAnnotatinKind).Any());
    }
}