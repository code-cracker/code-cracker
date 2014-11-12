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
            var variable = localDeclarationStatement.Declaration.Variables.Single();
            var variableSymbol = semanticModel.GetDeclaredSymbol(variable);
            return await MakeClassInitializerAsync(document, localDeclarationStatement, variableSymbol, semanticModel, cancellationToken);
        }

        private async Task<Document> MakeClassInitializerForAssignmentAsync(Document document, ExpressionStatementSyntax expressionStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var assignmentExpression = (AssignmentExpressionSyntax)expressionStatement.Expression;
            var variableSymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
            return await MakeClassInitializerAsync(document, expressionStatement, variableSymbol, semanticModel, cancellationToken);
        }

        private async Task<Document> MakeClassInitializerAsync(Document document, StatementSyntax statement, ISymbol variableSymbol, SemanticModel semanticModel, CancellationToken cancellationToken)
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
            var assignmentExpressions = ClassInitializerAnalyzer.FindAssingmentExpressions(semanticModel, statement, variableSymbol);
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