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
        public const string DiagnosticId = "CodeCracker.TernaryOperatorAnalyzer";
        internal const string Title = "User ternary operator";
        internal const string MessageFormat = "{0}";
        internal const string Category = "Syntax";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

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
                    var diagnostic = Diagnostic.Create(Rule, ifStatement.IfKeyword.GetLocation(), "You can use a ternary operator.");
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}