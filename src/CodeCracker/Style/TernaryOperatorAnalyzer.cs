using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TernaryOperatorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticIdForIfWithReturn = "CC0013";
        internal const string TitleForIfWithReturn = "User ternary operator";
        internal const string MessageFormatForIfWithReturn = "{0}";
        internal const string Category = SupportedCategories.Style;
        public const string DiagnosticIdForIfWithAssignment = "CC0014";
        internal const string TitleForIfWithAssignment = "User ternary operator";
        internal const string MessageFormatForIfWithAssignment = "{0}";

        internal static DiagnosticDescriptor RuleForIfWithReturn = new DiagnosticDescriptor(
            DiagnosticIdForIfWithReturn,
            TitleForIfWithReturn,
            MessageFormatForIfWithReturn,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(DiagnosticIdForIfWithReturn));

        internal static DiagnosticDescriptor RuleForIfWithAssignment = new DiagnosticDescriptor(
            DiagnosticIdForIfWithAssignment,
            TitleForIfWithAssignment,
            MessageFormatForIfWithAssignment,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(DiagnosticIdForIfWithAssignment));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleForIfWithReturn, RuleForIfWithAssignment); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.IfStatement);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
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