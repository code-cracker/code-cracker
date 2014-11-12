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
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerRethrowExceptionCodeFixProvider", LanguageNames.CSharp), Shared]
    public class ClassInitializerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(ClassInitializerAnalyzer.DiagnosticIdAssignment, ClassInitializerAnalyzer.DiagnosticIdLocalDeclaration);
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
            var declarationAssignment = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();
            var declarationDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            if (declarationAssignment != null)
                context.RegisterFix(CodeAction.Create("Use class initializer", c => MakeClassInitializerForAssignmentAsync(context.Document, declarationAssignment, c)), diagnostic);
            if (declarationDeclaration != null)
                context.RegisterFix(CodeAction.Create("Use class initializer", c => MakeClassInitializerForDeclarationAsync(context.Document, declarationDeclaration, c)), diagnostic);
        }

        private async Task<Document> MakeClassInitializerForDeclarationAsync(Document document, LocalDeclarationStatementSyntax localDeclarationStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var objectCreationExpressions = localDeclarationStatement.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().ToList();
            var objectCreationExpression = objectCreationExpressions.Single();
            var variable = localDeclarationStatement.Declaration.Variables.Single();
            var variableSymbol = semanticModel.GetDeclaredSymbol(variable);
            var blockParent = localDeclarationStatement.FirstAncestorOrSelf<BlockSyntax>();
            var isBefore = true;
            var assignmentExpressions = new List<ExpressionStatementSyntax>();
            foreach (var statement in blockParent.Statements)
            {
                if (isBefore)
                {
                    if (statement.Equals(localDeclarationStatement)) isBefore = false;
                }
                else
                {
                    var expressionStatement = statement as ExpressionStatementSyntax;
                    if (expressionStatement == null) break;
                    var assignmentExpression = expressionStatement.Expression as AssignmentExpressionSyntax;
                    if (assignmentExpression == null || !assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression)) break;
                    var memberAccess = assignmentExpression.Left as MemberAccessExpressionSyntax;
                    if (memberAccess == null || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)) break;
                    var memberIdentifier = memberAccess.Expression as IdentifierNameSyntax;
                    if (memberIdentifier == null) break;
                    var propertyIdentifier = memberAccess.Name as IdentifierNameSyntax;
                    if (propertyIdentifier == null) break;
                    if (!semanticModel.GetSymbolInfo(memberIdentifier).Symbol.Equals(variableSymbol)) break;
                    assignmentExpressions.Add(expressionStatement);
                }
            }
            var newBlockParent = SyntaxFactory.Block()
                .WithLeadingTrivia(blockParent.GetLeadingTrivia())
                .WithTrailingTrivia(blockParent.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newAssignmentExpressions = new List<ExpressionStatementSyntax>();
            for (int i = 0; i < blockParent.Statements.Count; i++)
            {
                var statement = blockParent.Statements[i];
                if (statement.Equals(localDeclarationStatement))
                {
                    var initializationExpressions = new List<AssignmentExpressionSyntax>();
                    foreach (var expressionStatement in assignmentExpressions)
                    {
                        var assignmentExpression = expressionStatement.Expression as AssignmentExpressionSyntax;
                        var memberAccess = assignmentExpression.Left as MemberAccessExpressionSyntax;
                        var propertyIdentifier = memberAccess.Name as IdentifierNameSyntax;
                        initializationExpressions.Add(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, propertyIdentifier, assignmentExpression.Right));
                    }
                    var initializers = SyntaxFactory.SeparatedList<ExpressionSyntax>(initializationExpressions);
                    var newObjectCreationExpression = objectCreationExpression.WithInitializer(
                        SyntaxFactory.InitializerExpression(
                            SyntaxKind.ObjectInitializerExpression,
                            SyntaxFactory.Token(SyntaxFactory.ParseLeadingTrivia(" "), SyntaxKind.OpenBraceToken, SyntaxFactory.ParseTrailingTrivia("\n")),
                            initializers,
                            SyntaxFactory.Token(SyntaxFactory.ParseLeadingTrivia(" "), SyntaxKind.CloseBraceToken, SyntaxFactory.ParseTrailingTrivia(""))
                        ))
                        .WithLeadingTrivia(objectCreationExpression.GetLeadingTrivia())
                        .WithTrailingTrivia(objectCreationExpression.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    var newLocalDeclarationStatement = localDeclarationStatement.ReplaceNode(objectCreationExpression, newObjectCreationExpression)
                        .WithLeadingTrivia(localDeclarationStatement.GetLeadingTrivia())
                        .WithTrailingTrivia(localDeclarationStatement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    newBlockParent = newBlockParent.AddStatements(newLocalDeclarationStatement);
                    i += initializationExpressions.Count;
                }
                else
                {
                    newBlockParent = newBlockParent.AddStatements(statement
                        .WithLeadingTrivia(statement.GetLeadingTrivia())
                        .WithTrailingTrivia(statement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(blockParent, newBlockParent);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private async Task<Document> MakeClassInitializerForAssignmentAsync(Document document, ExpressionStatementSyntax expressionStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var assignmentExpression = (AssignmentExpressionSyntax)expressionStatement.Expression;
            var variableSymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;

            var blockParent = expressionStatement.FirstAncestorOrSelf<BlockSyntax>();
            var isBefore = true;
            var assignmentExpressions = new List<ExpressionStatementSyntax>();
            foreach (var statement in blockParent.Statements)
            {
                if (isBefore)
                {
                    if (statement.Equals(expressionStatement)) isBefore = false;
                }
                else
                {
                    var theExpressionStatement = statement as ExpressionStatementSyntax;
                    if (theExpressionStatement == null) break;
                    var theAssignmentExpression = theExpressionStatement.Expression as AssignmentExpressionSyntax;
                    if (theAssignmentExpression == null || !theAssignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression)) break;
                    var memberAccess = theAssignmentExpression.Left as MemberAccessExpressionSyntax;
                    if (memberAccess == null || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)) break;
                    var memberIdentifier = memberAccess.Expression as IdentifierNameSyntax;
                    if (memberIdentifier == null) break;
                    var propertyIdentifier = memberAccess.Name as IdentifierNameSyntax;
                    if (propertyIdentifier == null) break;
                    assignmentExpressions.Add(theExpressionStatement);
                }
            }
            var newBlockParent = SyntaxFactory.Block()
                .WithLeadingTrivia(blockParent.GetLeadingTrivia())
                .WithTrailingTrivia(blockParent.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newAssignmentExpressions = new List<ExpressionStatementSyntax>();
            for (int i = 0; i < blockParent.Statements.Count; i++)
            {
                var statement = blockParent.Statements[i];
                if (statement.Equals(expressionStatement))
                {
                    var initializationExpressions = new List<AssignmentExpressionSyntax>();
                    foreach (var theExpressionStatement in assignmentExpressions)
                    {
                        var theAssignmentExpression = theExpressionStatement.Expression as AssignmentExpressionSyntax;
                        var memberAccess = theAssignmentExpression.Left as MemberAccessExpressionSyntax;
                        var propertyIdentifier = memberAccess.Name as IdentifierNameSyntax;
                        initializationExpressions.Add(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression, propertyIdentifier, theAssignmentExpression.Right));
                    }
                    var initializers = SyntaxFactory.SeparatedList<ExpressionSyntax>(initializationExpressions);
                    var objectCreationExpression = expressionStatement.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
                    var newObjectCreationExpression = objectCreationExpression.WithInitializer(
                        SyntaxFactory.InitializerExpression(
                            SyntaxKind.ObjectInitializerExpression,
                            SyntaxFactory.Token(SyntaxFactory.ParseLeadingTrivia(" "), SyntaxKind.OpenBraceToken, SyntaxFactory.ParseTrailingTrivia("\n")),
                            initializers,
                            SyntaxFactory.Token(SyntaxFactory.ParseLeadingTrivia(" "), SyntaxKind.CloseBraceToken, SyntaxFactory.ParseTrailingTrivia(""))
                        ))
                        .WithLeadingTrivia(objectCreationExpression.GetLeadingTrivia())
                        .WithTrailingTrivia(objectCreationExpression.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    var newExpressionStatement = expressionStatement.ReplaceNode(objectCreationExpression, newObjectCreationExpression)
                        .WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
                        .WithTrailingTrivia(expressionStatement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    newBlockParent = newBlockParent.AddStatements(newExpressionStatement);
                    i += initializationExpressions.Count;
                }
                else
                {
                    newBlockParent = newBlockParent.AddStatements(statement
                        .WithLeadingTrivia(statement.GetLeadingTrivia())
                        .WithTrailingTrivia(statement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(blockParent, newBlockParent);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}