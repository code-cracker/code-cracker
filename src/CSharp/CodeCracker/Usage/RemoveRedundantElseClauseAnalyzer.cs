using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveRedundantElseClauseAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Remove redundant else.";
        internal const string MessageFormat = "Remove redundant else";
        internal const string Category = SupportedCategories.Usage;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RemoveRedundantElseClause.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveRedundantElseClause));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ElseClause);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var @else = (ElseClauseSyntax)context.Node;
            if (((@else.Parent as IfStatementSyntax)?.Statement as BlockSyntax)?.Statements.Count == 0) return;
            if ((@else.Statement as BlockSyntax)?.Statements.Count > 0) return;
            var diagnostic = Diagnostic.Create(Rule, @else.GetLocation(), MessageFormat);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
