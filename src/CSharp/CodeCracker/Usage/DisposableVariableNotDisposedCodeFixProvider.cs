using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Usage
{
    [ExportCodeFixProvider("CodeCrackerCodeCrackerIfReturnTrueCodeFixProvider", LanguageNames.CSharp), Shared]
    public class DisposableVariableNotDisposedCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(DisposableVariableNotDisposedAnalyzer.DiagnosticId);
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
            var objectCreation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
            if (objectCreation != null)
                context.RegisterFix(CodeAction.Create($"Dispose object: '{objectCreation.Type.ToString()}'", c => CreateUsing(context.Document, objectCreation, c)), diagnostic);
        }

        private static async Task<Document> CreateUsing(Document document, ObjectCreationExpressionSyntax objectCreation, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            SyntaxNode newRoot, root = await document.GetSyntaxRootAsync();
            if (objectCreation.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                var assignmentExpression = (AssignmentExpressionSyntax)objectCreation.Parent;
                var statement = assignmentExpression.Parent as ExpressionStatementSyntax;
                var identitySymbol = (ILocalSymbol)semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
                newRoot = UsedOutsideParentBlock(semanticModel, statement, identitySymbol)
                    ? CreaterRootAddingDisposeToEndOfMethod(root, statement, identitySymbol)
                    : CreateRootWithUsing(root, statement, u => u.WithExpression(assignmentExpression));
            }
            else if (objectCreation.Parent.IsKind(SyntaxKind.EqualsValueClause) && objectCreation.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
            {
                var variableDeclarator = (VariableDeclaratorSyntax)objectCreation.Parent.Parent;
                var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent;
                var statement = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;
                newRoot = CreateRootWithUsing(root, statement, u => u.WithDeclaration(variableDeclaration));
            }
            else
            {
                newRoot = CreateRootWithUsing(root, (ExpressionStatementSyntax)objectCreation.Parent, u => u.WithExpression(objectCreation));
            }
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static SyntaxNode CreaterRootAddingDisposeToEndOfMethod(SyntaxNode root, ExpressionStatementSyntax statement, ILocalSymbol identitySymbol)
        {
            var method = statement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            var newDispose = ImplementsDisposableExplicitly(identitySymbol.Type)
                ? SyntaxFactory.ExpressionStatement(SyntaxFactory.InvocationExpression(
                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.ParenthesizedExpression(SyntaxFactory.CastExpression(SyntaxFactory.ParseName("System.IDisposable").WithAdditionalAnnotations(Simplifier.Annotation), SyntaxFactory.IdentifierName(identitySymbol.Name))),
                    SyntaxFactory.IdentifierName("Dispose"))))
                : SyntaxFactory.ParseStatement($"{identitySymbol.Name}.Dispose();");
            newDispose = newDispose.WithAdditionalAnnotations(Formatter.Annotation);
            var last = method.Body.Statements.Last();
            var newRoot = root.InsertNodesAfter(method.Body.Statements.Last(), new[] { newDispose });
            return newRoot;
        }

        private static SyntaxNode CreateRootWithUsing(SyntaxNode root, StatementSyntax statement, Func<UsingStatementSyntax, UsingStatementSyntax> updateUsing)
        {
            var statementsForUsing = GetChildStatementsAfter(statement);
            var statementsToReplace = new List<StatementSyntax> { statement };
            statementsToReplace.AddRange(statementsForUsing);
            var block = SyntaxFactory.Block(statementsForUsing);
            var usingStatement = updateUsing?.Invoke(CreateUsingStatement(statement, block));
            var newRoot = root.ReplaceNodes(statementsToReplace, (node, _) => node.Equals(statement) ? usingStatement : null);
            return newRoot;
        }

        private static UsingStatementSyntax CreateUsingStatement(StatementSyntax statement, BlockSyntax block)
        {
            return SyntaxFactory.UsingStatement(block)
                .WithLeadingTrivia(statement.GetLeadingTrivia())
                .WithTrailingTrivia(statement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
        }

        private static bool ImplementsDisposableExplicitly(ITypeSymbol type)
        {
            return type.GetMembers().Any(m => m.Name == "System.IDisposable.Dispose");
        }

        private static bool UsedOutsideParentBlock(SemanticModel semanticModel, StatementSyntax expressionStatement, ISymbol identitySymbol)
        {
            var block = expressionStatement.FirstAncestorOrSelf<BlockSyntax>();
            var method = expressionStatement.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            var methodBlock = method.Body;
            if (methodBlock.Equals(block)) return false;
            var collectionOfStatementsAfter = GetStatementsInBlocksAfter(block);
            foreach (var allStatementsAfterBlock in collectionOfStatementsAfter)
            {
                if (!allStatementsAfterBlock.Any()) continue;
                var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(allStatementsAfterBlock.First(), allStatementsAfterBlock.Last());
                if (!dataFlowAnalysis.Succeeded) continue;
                var isUsed = dataFlowAnalysis.ReadInside.Contains(identitySymbol)
                    || dataFlowAnalysis.WrittenInside.Contains(identitySymbol);
                if (isUsed) return true;
            }
            return false;
        }

        private static List<List<StatementSyntax>> GetStatementsInBlocksAfter(StatementSyntax node)
        {
            var collectionOfStatements = new List<List<StatementSyntax>>();
            var method = node.FirstAncestorOrSelf<MethodDeclarationSyntax>();
            if (method?.Body == null) return collectionOfStatements;
            var currentBlock = node.FirstAncestorOfType<BlockSyntax>();
            while (currentBlock != null)
            {
                var statements = new List<StatementSyntax>();
                foreach (var statement in currentBlock.Statements)
                    if (statement.SpanStart > node.SpanStart)
                        statements.Add(statement);
                if (statements.Any()) collectionOfStatements.Add(statements);
                if (method.Body.Equals(currentBlock)) break;
                currentBlock = currentBlock.FirstAncestorOfType<BlockSyntax>();
            }
            return collectionOfStatements;
        }

        private static IList<StatementSyntax> GetChildStatementsAfter(StatementSyntax node)
        {
            var block = node.FirstAncestorOrSelf<BlockSyntax>();
            var statements = block.Statements.Where(s => s.SpanStart > node.SpanStart).ToList();
            return statements;
        }
    }
}