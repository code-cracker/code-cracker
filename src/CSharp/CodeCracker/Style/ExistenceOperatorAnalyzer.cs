using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExistenceOperatorAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use the existence operator";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Style;
        const string Description = "The null-propagating operator allow for terse code to handle potentially null variables.";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ExistenceOperator.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ExistenceOperator));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyzer, SyntaxKind.IfStatement);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var ifStatement = context.Node as IfStatementSyntax;
            if (ifStatement?.Else == null) return;
            var notEqualExpression = ifStatement.Condition as BinaryExpressionSyntax;
            if (notEqualExpression == null
                || !notEqualExpression.IsKind(SyntaxKind.NotEqualsExpression)
                || !notEqualExpression.Left.IsKind(SyntaxKind.IdentifierName)
                || !notEqualExpression.Right.IsKind(SyntaxKind.NullLiteralExpression))
                return;
            var semanticModel = context.SemanticModel;
            var memberAccessIdentifier = GetMemberAccessIdentifierFromStatements(semanticModel,
                ifStatement.Statement.GetSingleStatementFromPossibleBlock(),
                ifStatement.Else.Statement.GetSingleStatementFromPossibleBlock());
            if (memberAccessIdentifier == null) return;
            CreateDiagnosticIfExpressionsMatch(
                context,
                ifStatement.IfKeyword.GetLocation(),
                semanticModel.GetSymbolInfo(notEqualExpression.Left),
                semanticModel.GetSymbolInfo(memberAccessIdentifier));
        }

        private static ExpressionSyntax GetMemberAccessIdentifierFromStatements(SemanticModel semanticModel, StatementSyntax statementInsideIf, StatementSyntax statementInsideElse)
        {
            var expressionIf = statementInsideIf as ExpressionStatementSyntax;
            var expressionElse = statementInsideElse as ExpressionStatementSyntax;
            var memberAccessExpression = expressionIf != null && expressionElse != null
                ? GetMemberAccessExpressionFromAssignment(semanticModel, expressionIf.Expression as AssignmentExpressionSyntax, expressionElse.Expression as AssignmentExpressionSyntax)
                : GetMemberAccessExpressionFromReturn(statementInsideIf as ReturnStatementSyntax, statementInsideElse as ReturnStatementSyntax);
            return memberAccessExpression?.Expression;
        }

        private static MemberAccessExpressionSyntax GetMemberAccessExpressionFromReturn(ReturnStatementSyntax returnIf, ReturnStatementSyntax returnElse)
        {
            if (returnIf?.Expression == null || returnElse?.Expression == null) return null;
            var nullLiteral = returnElse.Expression as LiteralExpressionSyntax;
            if (nullLiteral == null) return null;
            if (!nullLiteral.IsKind(SyntaxKind.NullLiteralExpression)) return null;
            var memberAccessExpression = returnIf.Expression as MemberAccessExpressionSyntax;
            return memberAccessExpression;
        }

        private static MemberAccessExpressionSyntax GetMemberAccessExpressionFromAssignment(SemanticModel semanticModel, AssignmentExpressionSyntax assignmentExpression, AssignmentExpressionSyntax nullLiteralAssignment)
        {
            if (assignmentExpression == null || nullLiteralAssignment == null
                || !assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression)
                || !assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression))
                return null;
            if (!nullLiteralAssignment.Right.IsKind(SyntaxKind.NullLiteralExpression)) return null;
            if (!nullLiteralAssignment.Left.IsKind(SyntaxKind.IdentifierName)) return null;
            if (!assignmentExpression.Left.IsKind(SyntaxKind.IdentifierName)) return null;
            var assignmentIdentifier = semanticModel.GetSymbolInfo(assignmentExpression.Left);
            var nullLiteralAssignmentIdentifier = semanticModel.GetSymbolInfo(nullLiteralAssignment.Left);
            if ((assignmentIdentifier.Symbol ?? nullLiteralAssignmentIdentifier.Symbol) == null) return null;
            if (!assignmentIdentifier.Equals(nullLiteralAssignmentIdentifier)) return null;
            var memberAccessExpression = assignmentExpression.Right as MemberAccessExpressionSyntax;
            return memberAccessExpression;
        }

        private static void CreateDiagnosticIfExpressionsMatch(SyntaxNodeAnalysisContext context, Location diagnosticLocation, SymbolInfo ifConditionIdentifier, SymbolInfo memberAccessIdentifier)
        {
            if ((ifConditionIdentifier.Symbol ?? memberAccessIdentifier.Symbol) == null) return;
            if (ifConditionIdentifier.Equals(memberAccessIdentifier))
            {
                var diagnostic = Diagnostic.Create(Rule, diagnosticLocation, "You can use the existence operator.");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}