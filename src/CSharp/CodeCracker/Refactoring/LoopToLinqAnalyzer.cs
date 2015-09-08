using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class LoopToLinqAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.LoopToLinq_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.LoopToLinq_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.LoopToLinq_Description), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Refactoring;
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.LoopToLinq.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.LoopToLinq));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeForEach, SyntaxKind.ForEachStatement);

        private static void AnalyzeForEach(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var fEach = (ForEachStatementSyntax)context.Node;
            var statement = GetStatement(fEach);
            if (statement == null) return;
            var collectionIdentifier = GetCollectionIdentifier(statement);
            if (collectionIdentifier == null) return;
            if (HasChangesOnCollection(context.SemanticModel, fEach, collectionIdentifier)) return;
            var diagnostic = Diagnostic.Create(Rule, fEach.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static StatementSyntax GetStatement(ForEachStatementSyntax fEach)
        {
            if (fEach.Statement.IsKind(SyntaxKind.Block))
            {
                var block = (BlockSyntax)fEach.Statement;
                switch (block.Statements.Count)
                {
                    case 1:
                        return block.Statements[0];
                    case 2:
                        var firstStatement = block.Statements[0];
                        var secondStatement = block.Statements[1];
                        if (!firstStatement.IsKind(SyntaxKind.LocalDeclarationStatement)) return null;
                        if (((LocalDeclarationStatementSyntax)firstStatement).Declaration?.Variables.Count > 1) return null;
                        return secondStatement;
                    case 0:
                    default:
                        return null;
                }
            }
            else
            {
                return fEach.Statement;
            }
        }

        private static IdentifierNameSyntax GetCollectionIdentifier(StatementSyntax statementInFor)
        {
            if (statementInFor.IsKind(SyntaxKind.ExpressionStatement))
            {
                return GetCollectionIdentifier((ExpressionStatementSyntax)statementInFor);
            }
            else if (statementInFor.IsKind(SyntaxKind.IfStatement))
            {
                var ifStatement = (IfStatementSyntax)statementInFor;
                var statementInIf = ifStatement.Statement.GetSingleStatementFromPossibleBlock();
                if (statementInIf == null) return null;
                return GetCollectionIdentifier((ExpressionStatementSyntax)statementInIf);
            }
            else return null;
        }

        private static IdentifierNameSyntax GetCollectionIdentifier(ExpressionStatementSyntax expressionStatement)
        {
            var expression = expressionStatement.Expression;
            if (!expression.IsKind(SyntaxKind.InvocationExpression)) return null;
            var invocation = (InvocationExpressionSyntax)expression;
            if (!invocation.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression)) return null;
            var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
            if (memberAccess.Name.ToString() != "Add") return null;
            if (invocation.ArgumentList.Arguments.Count != 1) return null;
            if (!memberAccess.Expression.IsKind(SyntaxKind.IdentifierName)) return null;
            var collectionIdentifier = (IdentifierNameSyntax)memberAccess.Expression;
            return collectionIdentifier;
        }

        private static bool HasChangesOnCollection(SemanticModel semanticModel, ForEachStatementSyntax fEach, IdentifierNameSyntax collectionIdentifier)
        {
            var targetCollectionSymbol = semanticModel.GetSymbolInfo(collectionIdentifier).Symbol;
            var targetCollectionDeclarator = targetCollectionSymbol?.DeclaringSyntaxReferences.First()?.GetSyntax() as VariableDeclaratorSyntax;
            if (targetCollectionDeclarator == null) return true;
            if (targetCollectionDeclarator.Initializer != null)
            {
                if (!targetCollectionDeclarator.Initializer.Value.IsKind(SyntaxKind.ObjectCreationExpression)) return true;
                var initializerObjectCreation = (ObjectCreationExpressionSyntax)targetCollectionDeclarator.Initializer.Value;
                if (initializerObjectCreation.Initializer != null) return true;
            }
            var block = targetCollectionDeclarator.FirstAncestorOrSelfOfType<BlockSyntax>();
            if (block == null) return true;
            var bodyStatements = block.Statements;
            var start = bodyStatements.IndexOf(targetCollectionDeclarator.FirstAncestorOrSelfThatIsAStatement());
            var end = bodyStatements.IndexOf(fEach);
            for (int i = start; i < end; i++)
            {
                var candidateStatement = bodyStatements[i];
                var candidateInvocations1 = candidateStatement.DescendantNodes().OfType<InvocationExpressionSyntax>().ToList();
                var candidateInvocations2 = candidateStatement.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Select(inv => inv.Expression).ToList();
                var candidateInvocations3 = candidateStatement.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Select(inv => inv.Expression)
                    .Where(e => e.IsKind(SyntaxKind.SimpleMemberAccessExpression)).ToList();
                var candidateInvocations4 = candidateStatement.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Select(inv => inv.Expression)
                    .Where(e => e.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    .Select(e => ((MemberAccessExpressionSyntax)e).Expression).ToList();
                var candidateInvocations = candidateStatement.DescendantNodes().OfType<InvocationExpressionSyntax>()
                    .Select(inv => inv.Expression)
                    .Where(e => e.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                    .Where(e => targetCollectionSymbol.Equals(semanticModel.GetSymbolInfo(((MemberAccessExpressionSyntax)e).Expression).Symbol));
                if (candidateInvocations.Any()) return true;
            }
            return false;
        }
    }
}