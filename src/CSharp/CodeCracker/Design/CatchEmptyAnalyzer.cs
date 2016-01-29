using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CatchEmptyAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Your catch maybe include some Exception";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Design;
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.CatchEmpty.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.CatchEmpty));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.CatchClause);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var catchStatement = (CatchClauseSyntax)context.Node;

            if (catchStatement == null || catchStatement.Declaration != null) return;
            if (catchStatement.Block?.Statements.Count == 0) return; // there is another analizer for this: EmptyCatchBlock

            if (catchStatement.Block != null)
            {
                // Allow empty catch when the block ends with a throw.
                var controlFlow = context.SemanticModel.AnalyzeControlFlow(catchStatement.Block);
                if (!controlFlow.EndPointIsReachable &&
                    controlFlow.ExitPoints.All(i => i.IsKind(SyntaxKind.ThrowStatement))) return;
            }
            var diagnostic = Diagnostic.Create(Rule, catchStatement.GetLocation(), "Consider put an Exception Class in catch.");
            context.ReportDiagnostic(diagnostic);
        }
    }
}