﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeCracker.Style.Style
{
    [ExportCodeFixProvider("CodeCrackerConvertToExpressionBodiedMemberCodeFixProvider", LanguageNames.CSharp), Shared]
    public class ConvertToExpressionBodiedMemberCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(ConvertToExpressionBodiedMemberAnalyzer.DiagnosticId);
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
            var message = "Convert to an expression bodied member.";

            var methodDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<BaseMethodDeclarationSyntax>().FirstOrDefault();
            if (methodDeclaration != null)
            {
                context.RegisterFix(CodeAction.Create(message, c => ConvertToExpressionBodiedMemberAsync(context.Document, methodDeclaration, c)), diagnostic);
            }
            else
            {
                var indexerDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IndexerDeclarationSyntax>().First();
                context.RegisterFix(CodeAction.Create(message, c => ConvertToExpressionBodiedMemberAsync(context.Document, indexerDeclaration, c)), diagnostic);
            }
        }

        private async Task<Document> ConvertToExpressionBodiedMemberAsync(
            Document document,
            IndexerDeclarationSyntax indexerDeclaration,
            CancellationToken c
            )
        {
            var accessors = indexerDeclaration.AccessorList.Accessors;
            var body = accessors[0].Body;
            var returnStatement = body.Statements[0] as ReturnStatementSyntax;

            var arrowExpression = SyntaxFactory.ArrowExpressionClause(
                returnStatement.Expression
            );

            var newIndexerDeclaration = indexerDeclaration
                .WithAccessorList(null)
                .WithExpressionBody(arrowExpression)
                .WithSemicolon(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                .WithAdditionalAnnotations(Formatter.Annotation);

            return await ReplaceNode(document, indexerDeclaration, newIndexerDeclaration);
        }

        public async Task<Document> ReplaceNode(Document document, SyntaxNode @old, SyntaxNode @new)
        {
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(@old, @new);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private async Task<Document> ConvertToExpressionBodiedMemberAsync(
            Document document,
            BaseMethodDeclarationSyntax declaration, 
            CancellationToken c
            )
        {
            var body = declaration.Body;
            var returnStatement = body.Statements[0] as ReturnStatementSyntax;

            var arrowExpression = SyntaxFactory.ArrowExpressionClause(
                returnStatement.Expression
                );

            var newDeclaration = declaration;

            newDeclaration = ((dynamic)newDeclaration)
                .WithBody(null)
                .WithExpressionBody(arrowExpression)
                .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));

            newDeclaration = newDeclaration
                .WithAdditionalAnnotations(Formatter.Annotation);

            return await ReplaceNode(document, declaration, newDeclaration);
        }
    }
}
