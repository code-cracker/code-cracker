using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyCatchBlockAnalyzer : DiagnosticAnalyzer
    {
        private static readonly string diagnosticId = DiagnosticId.EmptyCatchBlock.ToDiagnosticId();
        internal const string Title = "Catch block cannot be empty";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Design;
        const string Description = "An empty catch block suppress all errors and shouldn't be used.\r\n"
            +"If the error is expected consider logging it or changing the control flow such that it is explicit.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(diagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(diagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.CatchClause);

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var catchStatement = (CatchClauseSyntax)context.Node;
            if (catchStatement?.Block?.Statements.Count != 0) return;
            var diagnostic = Diagnostic.Create(Rule, catchStatement.GetLocation(), "Empty Catch Block.");
            context.ReportDiagnostic(diagnostic);
        }
    }
}