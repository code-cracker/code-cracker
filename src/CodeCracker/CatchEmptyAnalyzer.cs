using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CatchEmptyAnalyser : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.CatchEmptyAnalyser";
        internal const string Title = "Your catch maybe include some Exception";
        internal const string MessageFormat = "{0}";
        internal const string Category = "Syntax";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.CatchClause);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var catchStatement = (CatchClauseSyntax)context.Node;

            if (catchStatement.Declaration != null) return;

            var diagnostic = Diagnostic.Create(Rule, catchStatement.GetLocation(), "Consider put an Exception Class in catch.");
            context.ReportDiagnostic(diagnostic);
        }
    }
}