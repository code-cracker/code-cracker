using Microsoft.CodeAnalysis;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SortUsingsAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Sort by length.";
        internal const string MessageFormat = "Sort Using directives by length";
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.SortUsings.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.SortUsings));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.CompilationUnit);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var root = (CompilationUnitSyntax)context.Node;
            if (root.Usings.Count > 0)
                context.ReportDiagnostic(Diagnostic.Create(Rule, root.GetLocation()));
        }
    }
}
