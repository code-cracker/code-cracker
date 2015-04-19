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

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider("CodeCrackerRethrowExceptionCodeFixProvider", LanguageNames.CSharp), Shared]
    public class ObjectInitializerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ObjectInitializer_Assignment.ToDiagnosticId(), DiagnosticId.ObjectInitializer_LocalDeclaration.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var expressionStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();
            var localDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            if (expressionStatement != null)
                context.RegisterCodeFix(CodeAction.Create("Use object initializer", c => MakeObjectInitializerAsync(context.Document, expressionStatement, c)), diagnostic);
            else if (localDeclaration != null)
                context.RegisterCodeFix(CodeAction.Create("Use object initializer", c => MakeObjectInitializerAsync(context.Document, localDeclaration, c)), diagnostic);
        }

        private async Task<Document> MakeObjectInitializerAsync(Document document, LocalDeclarationStatementSyntax localDeclarationStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var variable = localDeclarationStatement.Declaration.Variables.Single();
            var variableSymbol = semanticModel.GetDeclaredSymbol(variable);
            return await MakeObjectInitializerAsync(document, localDeclarationStatement, variableSymbol, semanticModel, cancellationToken);
        }

        private async Task<Document> MakeObjectInitializerAsync(Document document, ExpressionStatementSyntax expressionStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var assignmentExpression = (AssignmentExpressionSyntax)expressionStatement.Expression;
            var variableSymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
            return await MakeObjectInitializerAsync(document, expressionStatement, variableSymbol, semanticModel, cancellationToken);
        }

        private async Task<Document> MakeObjectInitializerAsync(Document document, StatementSyntax statement, ISymbol variableSymbol, SemanticModel semanticModel, CancellationToken cancellationToken)
        {
            var blockParent = statement.FirstAncestorOrSelf<BlockSyntax>();
            var objectCreationExpression = statement.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            var newBlockParent = CreateNewBlockParent(statement, semanticModel, objectCreationExpression, variableSymbol);
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var newRoot = root.ReplaceNode(blockParent, newBlockParent);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static BlockSyntax CreateNewBlockParent(StatementSyntax statement, SemanticModel semanticModel, ObjectCreationExpressionSyntax objectCreationExpression, ISymbol variableSymbol)
        {
            var blockParent = statement.FirstAncestorOrSelf<BlockSyntax>();
            var assignmentExpressions = ObjectInitializerAnalyzer.FindAssignmentExpressions(semanticModel, statement, variableSymbol);
            var newBlockParent = SyntaxFactory.Block()
                .WithLeadingTrivia(blockParent.GetLeadingTrivia())
                .WithTrailingTrivia(blockParent.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newAssignmentExpressions = new List<ExpressionStatementSyntax>();
            for (int i = 0; i < blockParent.Statements.Count; i++)
            {
                var blockStatement = blockParent.Statements[i];
                if (blockStatement.Equals(statement))
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
                    var newLocalDeclarationStatement = statement.ReplaceNode(objectCreationExpression, newObjectCreationExpression)
                        .WithLeadingTrivia(statement.GetLeadingTrivia())
                        .WithTrailingTrivia(statement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    newBlockParent = newBlockParent.AddStatements(newLocalDeclarationStatement);
                    i += initializationExpressions.Count;
                }
                else
                {
                    newBlockParent = newBlockParent.AddStatements(blockStatement
                        .WithLeadingTrivia(blockStatement.GetLeadingTrivia())
                        .WithTrailingTrivia(blockStatement.GetTrailingTrivia())
                        .WithAdditionalAnnotations(Formatter.Annotation));
                }
            }
            return newBlockParent;
        }
    }
}