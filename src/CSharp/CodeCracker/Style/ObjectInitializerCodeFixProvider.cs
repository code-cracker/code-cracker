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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ObjectInitializerCodeFixProvider)), Shared]
    public class ObjectInitializerCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ObjectInitializer_Assignment.ToDiagnosticId(), DiagnosticId.ObjectInitializer_LocalDeclaration.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Use object initializer", c => MakeObjectInitializerAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> MakeObjectInitializerAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var expressionStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ExpressionStatementSyntax>().FirstOrDefault();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            if (expressionStatement != null)
                return MakeObjectInitializer(document, root, semanticModel, expressionStatement);
            var localDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().FirstOrDefault();
            return MakeObjectInitializer(document, root, semanticModel, localDeclaration);
        }

        private static Document MakeObjectInitializer(Document document, SyntaxNode root, SemanticModel semanticModel, LocalDeclarationStatementSyntax localDeclarationStatement)
        {
            var variable = localDeclarationStatement.Declaration.Variables.Single();
            var variableSymbol = semanticModel.GetDeclaredSymbol(variable);
            return MakeObjectInitializer(document, root, localDeclarationStatement, variableSymbol, semanticModel);
        }

        private static Document MakeObjectInitializer(Document document, SyntaxNode root, SemanticModel semanticModel, ExpressionStatementSyntax expressionStatement)
        {
            var assignmentExpression = (AssignmentExpressionSyntax)expressionStatement.Expression;
            var variableSymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
            return MakeObjectInitializer(document, root, expressionStatement, variableSymbol, semanticModel);
        }

        private static Document MakeObjectInitializer(Document document, SyntaxNode root, StatementSyntax statement, ISymbol variableSymbol, SemanticModel semanticModel)
        {
            var blockParent = statement.FirstAncestorOrSelf<BlockSyntax>();
            var objectCreationExpression = statement.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().Single();
            var newBlockParent = CreateNewBlockParent(statement, semanticModel, objectCreationExpression, variableSymbol);
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