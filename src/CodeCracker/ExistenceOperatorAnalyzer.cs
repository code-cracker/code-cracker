using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System;
using System.Linq;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ExistenceOperatorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0018";
        internal const string Title = "Use the existence operator";
        internal const string MessageFormat = "{0}";
        internal const string Category = "Syntax";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyzer, SyntaxKind.IfStatement);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var ifStatement = context.Node as IfStatementSyntax;
            if (ifStatement == null) return;

            //var diagnostic = Diagnostic.Create(Rule, ifStatement.IfKeyword.GetLocation(), "You can use the existence operator.");
            //context.ReportDiagnostic(diagnostic);
        }
    }
}