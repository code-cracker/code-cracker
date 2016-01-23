using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
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

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(LoopToLinqCodeFixProvider)), Shared]
    public class LoopToLinqCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.LoopToLinq.ToDiagnosticId());

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(Resources.LoopToLinq_Title, c => CreateLinqAsync(context.Document, diagnostic, c), nameof(LoopToLinqCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> CreateLinqAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var fEach = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ForEachStatementSyntax>().FirstOrDefault();
            var queryInfo = new QueryInfo(fEach);
            var queryExpression = queryInfo.CreateQuery();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var targetCollectionDeclarator = GetCollectionDeclarator(semanticModel, queryInfo.Invocation);
            var newQueryLocalDeclaration = CreateNewQueryLocalDeclaration(queryExpression, targetCollectionDeclarator, fEach);
            var variableDeclaration = (VariableDeclarationSyntax)targetCollectionDeclarator.Parent;
            var localDeclarationForList = (LocalDeclarationStatementSyntax)variableDeclaration.Parent;
            var newLocalDeclarationForList = CreateNewLocalDeclarationForList(variableDeclaration, targetCollectionDeclarator, localDeclarationForList);
            var newRoot = root.ReplaceNodes(new SyntaxNode[] { fEach, localDeclarationForList },
                (node, _) => node.Equals(fEach) ? newQueryLocalDeclaration : newLocalDeclarationForList);
            newRoot = AddUsingSystemLinq(root, newRoot);
            return document.WithSyntaxRoot(newRoot);
        }

        private static LocalDeclarationStatementSyntax CreateNewQueryLocalDeclaration(
            QueryExpressionSyntax queryExpression,
            VariableDeclaratorSyntax targetCollectionDeclarator,
            ForEachStatementSyntax fEach)
        {
            var newCollectionDeclarator = targetCollectionDeclarator.WithInitializer(SyntaxFactory.EqualsValueClause(queryExpression));
            var newVariableDeclaration = SyntaxFactory.VariableDeclaration(SyntaxFactory.ParseTypeName("var"), SyntaxFactory.SeparatedList(new[] { newCollectionDeclarator }));
            var newQueryLocalDeclaration = SyntaxFactory.LocalDeclarationStatement(newVariableDeclaration)
                .WithLeadingTrivia(fEach.GetLeadingTrivia())
                .WithTrailingTrivia(fEach.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            return newQueryLocalDeclaration;
        }

        private static LocalDeclarationStatementSyntax CreateNewLocalDeclarationForList(
            VariableDeclarationSyntax variableDeclaration,
            VariableDeclaratorSyntax targetCollectionDeclarator,
            LocalDeclarationStatementSyntax localDeclarationForList)
        {
            if (variableDeclaration.Variables.Count <= 1) return null;
            var variablesWithoutCollectionDeclarator = variableDeclaration.Variables.Remove(targetCollectionDeclarator);
            var replacingVariableDeclaration = SyntaxFactory.VariableDeclaration(variableDeclaration.Type, variablesWithoutCollectionDeclarator);
            var newLocalDeclarationForList = SyntaxFactory.LocalDeclarationStatement(replacingVariableDeclaration)
                .WithLeadingTrivia(localDeclarationForList.GetLeadingTrivia())
                .WithTrailingTrivia(localDeclarationForList.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            return newLocalDeclarationForList;
        }

        private static VariableDeclaratorSyntax GetCollectionDeclarator(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
        {
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
            var targetCollectionSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol;
            var targetCollectionDeclarator = (VariableDeclaratorSyntax)targetCollectionSymbol.DeclaringSyntaxReferences.First().GetSyntax();
            return targetCollectionDeclarator;
        }

        private static CompilationUnitSyntax AddUsingSystemLinq(CompilationUnitSyntax root, CompilationUnitSyntax newRoot)
        {
            var isUsingSystemnLinq = root.Usings.Any(u => u.Name.GetText().ToString() == "System.Linq");
            if (!isUsingSystemnLinq)
                newRoot = newRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")));
            return newRoot;
        }

        private class QueryInfo
        {
            public QueryInfo(ForEachStatementSyntax fEach)
            {
                this.fEach = fEach;
                GetQueryValues();
            }
            private readonly ForEachStatementSyntax fEach;
            private InvocationExpressionSyntax invocation;
            private ExpressionSyntax whereExpression;
            private SyntaxToken fromIdentifier;
            private ExpressionSyntax selectExpression;
            private VariableDeclaratorSyntax letVariable;

            private void GetQueryValues()
            {
                fromIdentifier = fEach.Identifier;
                var statementInFor = GetStatement(fEach);
                if (statementInFor.IsKind(SyntaxKind.ExpressionStatement))
                {
                    GetInvocation((ExpressionStatementSyntax)statementInFor);
                }
                else if (statementInFor.IsKind(SyntaxKind.IfStatement))
                {
                    var ifStatement = (IfStatementSyntax)statementInFor;
                    whereExpression = ifStatement.Condition;
                    var statementInIf = ifStatement.Statement.GetSingleStatementFromPossibleBlock();
                    GetInvocation((ExpressionStatementSyntax)statementInIf);
                }
                selectExpression = invocation.ArgumentList.Arguments[0].Expression;
            }

            private StatementSyntax GetStatement(ForEachStatementSyntax fEach)
            {
                if (fEach.Statement.IsKind(SyntaxKind.Block))
                {
                    var block = (BlockSyntax)fEach.Statement;
                    if (block.Statements.Count == 1)
                    {
                        return block.Statements[0];
                    }
                    else//2
                    {
                        letVariable = ((LocalDeclarationStatementSyntax)block.Statements[0]).Declaration.Variables[0];
                        var secondStatement = block.Statements[1];
                        return secondStatement;
                    }
                }
                else
                {
                    return fEach.Statement;
                }
            }

            private void GetInvocation(ExpressionStatementSyntax expressionStatement)
            {
                var expression = expressionStatement.Expression;
                invocation = (InvocationExpressionSyntax)expression;
            }

            public QueryExpressionSyntax CreateQuery()
            {
                var queryBody = SyntaxFactory.QueryBody(SyntaxFactory.SelectClause(selectExpression));
                if (letVariable != null) queryBody = queryBody.AddClauses(SyntaxFactory.LetClause(letVariable.Identifier, letVariable.Initializer.Value));
                if (whereExpression != null) queryBody = queryBody.AddClauses(SyntaxFactory.WhereClause(whereExpression));
                var linqExpression = SyntaxFactory.QueryExpression(
                    SyntaxFactory.FromClause(fromIdentifier, fEach.Expression),
                    queryBody);
                return linqExpression;
            }

            public InvocationExpressionSyntax Invocation => invocation;
        }
    }
}