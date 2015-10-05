using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TernaryOperatorAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use ternary operator";
        internal const string MessageFormatForIfWithReturn = "{0}";
        internal const string Category = SupportedCategories.Style;
        internal const string MessageFormatForIfWithAssignment = "{0}";

        internal static readonly DiagnosticDescriptor RuleForIfWithReturn = new DiagnosticDescriptor(
            DiagnosticId.TernaryOperator_Return.ToDiagnosticId(),
            Title,
            MessageFormatForIfWithReturn,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.TernaryOperator_Return));

        internal static readonly DiagnosticDescriptor RuleForIfWithAssignment = new DiagnosticDescriptor(
            DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId(),
            Title,
            MessageFormatForIfWithAssignment,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.TernaryOperator_Assignment));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleForIfWithReturn, RuleForIfWithAssignment);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.IfStatement);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var ifStatement = context.Node as IfStatementSyntax;
            if (ifStatement == null) return;
            if (ifStatement.Else == null) return;
            var blockIf = ifStatement.Statement as BlockSyntax;
            var blockElse = ifStatement.Else.Statement as BlockSyntax;
            if ((blockIf == null || blockIf.Statements.Count == 1) &&
                (blockElse == null || blockElse.Statements.Count == 1))
            {
                var statementInsideIf = ifStatement.Statement is BlockSyntax ? ((BlockSyntax)ifStatement.Statement).Statements.Single() : ifStatement.Statement;
                var elseStatement = ifStatement.Else;
                var statementInsideElse = elseStatement.Statement is BlockSyntax ? ((BlockSyntax)elseStatement.Statement).Statements.Single() : elseStatement.Statement;
                if (statementInsideIf is ReturnStatementSyntax && statementInsideElse is ReturnStatementSyntax)
                {
                    var diagnostic = Diagnostic.Create(RuleForIfWithReturn, ifStatement.IfKeyword.GetLocation(), "You can use a ternary operator.");
                    context.ReportDiagnostic(diagnostic);
                    return;
                }
                var expressionInsideIf = statementInsideIf as ExpressionStatementSyntax;
                var expressionInsideElse = statementInsideElse as ExpressionStatementSyntax;
                if (expressionInsideIf != null && expressionInsideIf.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression) && expressionInsideElse != null && expressionInsideElse.Expression.IsKind(SyntaxKind.SimpleAssignmentExpression))
                {
                    var assignmentExpressionInsideIf = (AssignmentExpressionSyntax)expressionInsideIf.Expression;
                    var assignmentExpressionInsideElse = (AssignmentExpressionSyntax)expressionInsideElse.Expression;
                    var variableIdentifierInsideIf = assignmentExpressionInsideIf.Left as IdentifierNameSyntax;
                    var variableIdentifierInsideElse = assignmentExpressionInsideElse.Left as IdentifierNameSyntax;
                    if (variableIdentifierInsideIf == null || variableIdentifierInsideElse == null
                        || variableIdentifierInsideIf.Identifier.Text != variableIdentifierInsideElse.Identifier.Text) return;
                    var diagnostic = Diagnostic.Create(RuleForIfWithAssignment, ifStatement.IfKeyword.GetLocation(), "You can use a ternary operator.");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}