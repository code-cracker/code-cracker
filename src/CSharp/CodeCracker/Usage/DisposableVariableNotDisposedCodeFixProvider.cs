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

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposableVariableNotDisposedCodeFixProvider)), Shared]
    public class DisposableVariableNotDisposedCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.DisposableVariableNotDisposed.ToDiagnosticId());
        public readonly static string MessageFormat = "Dispose object: '{0}'";

        public sealed override FixAllProvider GetFixAllProvider() => DisposableVariableNotDisposedFixAllProvider.Instance;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            if (diagnostic.Properties.ContainsKey(DisposableVariableNotDisposedAnalyzer.cantFix)) return Task.FromResult(0);
            var title = string.Format(MessageFormat, diagnostic.Properties["typeName"]);
            context.RegisterCodeFix(CodeAction.Create(title, c => CreateUsingAsync(context.Document, diagnostic, c), nameof(DisposableVariableNotDisposedCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> CreateUsingAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var objectCreation = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var newRoot = CreateUsing(root, objectCreation, semanticModel);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        public static SyntaxNode CreateUsing(SyntaxNode root, ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel)
        {
            SyntaxNode newRoot;
            if (objectCreation.Parent.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                var assignmentExpression = (AssignmentExpressionSyntax)objectCreation.Parent;
                var statement = assignmentExpression.Parent as ExpressionStatementSyntax;
                var identitySymbol = (ILocalSymbol)semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
                newRoot = UsedOutsideParentBlock(semanticModel, statement, identitySymbol)
                    ? CreateRootAddingDisposeToEndOfMethod(root, statement, identitySymbol)
                    : CreateRootWithUsing(root, statement, u => u.WithExpression(assignmentExpression));
            }
            else if (objectCreation.Parent.IsKind(SyntaxKind.EqualsValueClause) && objectCreation.Parent.Parent.IsKind(SyntaxKind.VariableDeclarator))
            {
                var variableDeclarator = (VariableDeclaratorSyntax)objectCreation.Parent.Parent;
                var variableDeclaration = (VariableDeclarationSyntax)variableDeclarator.Parent;
                var statement = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;
                newRoot = CreateRootWithUsing(root, statement, u => u.WithDeclaration(variableDeclaration));
            }
            else if (objectCreation.Parent.IsKind(SyntaxKind.Argument))
            {
                var identifierName = GetIdentifierName(objectCreation, semanticModel);

                var variableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.IdentifierName(@"var"))
                    .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(identifierName))
                    .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.Token(SyntaxKind.EqualsToken), objectCreation))));

                var arg = objectCreation.Parent as ArgumentSyntax;
                var args = objectCreation.Parent.Parent as ArgumentListSyntax;
                var newArgs = args.ReplaceNode(arg, arg.WithExpression(SyntaxFactory.IdentifierName(identifierName)));

                StatementSyntax statement = objectCreation.FirstAncestorOfType<ExpressionStatementSyntax>();
                if (statement != null)
                {
                    var exprStatement = statement.ReplaceNode(args, newArgs);
                    var newUsingStatment = CreateUsingStatement(exprStatement, SyntaxFactory.Block(exprStatement))
                        .WithDeclaration(variableDeclaration);
                    return root.ReplaceNode(statement, newUsingStatment);
                }

                statement = (StatementSyntax)objectCreation.Ancestors().First(node => node is StatementSyntax);
                var newStatement = statement.ReplaceNode(args, newArgs);
                var statementsForUsing = new[] { newStatement }.Concat(GetChildStatementsAfter(statement));
                var usingBlock = SyntaxFactory.Block(statementsForUsing);
                var usingStatement = CreateUsingStatement(newStatement, usingBlock)
                    .WithDeclaration(variableDeclaration);
                var statementsToReplace = new List<StatementSyntax> { statement };
                statementsToReplace.AddRange(statementsForUsing.Skip(1));
                newRoot = root.ReplaceNodes(statementsToReplace, (node, _) => node.Equals(statement) ? usingStatement : null);
            }
            else
            {
                newRoot = CreateRootWithUsing(root, (ExpressionStatementSyntax)objectCreation.Parent, u => u.WithExpression(objectCreation));
            }
            return newRoot;
        }

        private static string GetIdentifierName(ObjectCreationExpressionSyntax objectCreation, SemanticModel semanticModel)
        {
            var identifierName = "disposableObject";

            var type = objectCreation.Type;
            if (type.IsKind(SyntaxKind.QualifiedName))
            {
                var name = (QualifiedNameSyntax)type;
                identifierName = LowerCaseFirstLetter(name.Right.Identifier.ValueText);
            }
            else if (type is SimpleNameSyntax)
            {
                var name = (SimpleNameSyntax)type;
                identifierName = LowerCaseFirstLetter(name.Identifier.ValueText);
            }

            var confilctingNames = from symbol in semanticModel.LookupSymbols(objectCreation.SpanStart)
                                   let symbolIdentifierName = symbol?.ToDisplayParts().LastOrDefault(AnalyzerExtensions.IsName).ToString()
                                   where symbolIdentifierName != null && symbolIdentifierName.StartsWith(identifierName)
                                   select symbolIdentifierName;

            var identifierPostFix = 0;
            while (confilctingNames.Any(p => p == identifierName + ++identifierPostFix)) { }
            return identifierName + (identifierPostFix == 0 ? "" : identifierPostFix.ToString());
        }

        private static string LowerCaseFirstLetter(string name) => char.ToLowerInvariant(name[0]) + name.Substring(1);

        private static SyntaxNode CreateRootAddingDisposeToEndOfMethod(SyntaxNode root, ExpressionStatementSyntax statement, ILocalSymbol identitySymbol)
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