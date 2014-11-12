using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TernaryOperatorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticIdForIfWithReturn = "CodeCracker.TernaryOperatorWithReturnAnalyzer";
        internal const string TitleForIfWithReturn = "User ternary operator";
        internal const string MessageFormatForIfWithReturn = "{0}";
        internal const string Category = "Syntax";
        public const string DiagnosticIdForIfWithAssignment = "CodeCracker.TernaryOperatorWithAssignmentAnalyzer";
        internal const string TitleForIfWithAssignment = "User ternary operator";
        internal const string MessageFormatForIfWithAssignment = "{0}";

        internal static DiagnosticDescriptor RuleForIfWithReturn = new DiagnosticDescriptor(DiagnosticIdForIfWithReturn, TitleForIfWithReturn, MessageFormatForIfWithReturn, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);
        internal static DiagnosticDescriptor RuleForIfWithAssignment = new DiagnosticDescriptor(DiagnosticIdForIfWithAssignment, TitleForIfWithAssignment, MessageFormatForIfWithAssignment, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

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
            if (((blockIf ?? blockElse) == null) ||
                (blockIf.Statements.Count == 1 && blockElse.Statements.Count == 1))
            {
                //add diagnostic, only 1 statement for if and else
                //or not one direct statement, but could be one in each block, lets check
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