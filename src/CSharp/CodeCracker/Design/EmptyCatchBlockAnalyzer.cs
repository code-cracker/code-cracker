using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyCatchBlockAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Catch block cannot be empty";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Design;
        const string Description = "An empty catch block suppress all errors and shouldn't be used.\r\n"
            +"If the error is expected consider logging it or changing the control flow such that it is explicit.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId.EmptyCatchBlock.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.EmptyCatchBlock));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.CatchClause);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var catchStatement = (CatchClauseSyntax)context.Node;
            if (catchStatement?.Block?.Statements.Count != 0) return;
            var diagnostic = Diagnostic.Create(Rule, catchStatement.GetLocation(), "Empty Catch Block.");
            context.ReportDiagnostic(diagnostic);
        }
    }
}