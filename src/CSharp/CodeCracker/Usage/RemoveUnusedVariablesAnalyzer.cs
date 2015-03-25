using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using System.Linq;
using Microsoft.CodeAnalysis.Text;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveUnusedVariablesAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Variable Unused";
        internal const string MessageFormat = "The variable {0} is never used";
        const string Description = "When a variable declares and does not use it might bring incorrect conclusions.";
        internal const string Category = SupportedCategories.Usage;
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RemoveUnusedVariables.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: false,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveUnusedVariables));


        public static FixAllProvider Instance { get; internal set; }

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNode, new[] { SyntaxKind.ConstructorDeclaration, SyntaxKind.MethodDeclaration });


        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var methodOrConstructor = context.Node as BaseMethodDeclarationSyntax;
            if (methodOrConstructor == null) return;

            var semanticModel = context.SemanticModel;

            var variables = (from node in methodOrConstructor.DescendantNodes()
                             where node.IsKind(SyntaxKind.LocalDeclarationStatement)
                             select node as LocalDeclarationStatementSyntax);

            if (variables == null || !variables.Any()) return;
            if (methodOrConstructor?.Body?.Statements.FirstOrDefault() == null || methodOrConstructor?.Body?.Statements.LastOrDefault() == null) return;

            var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(methodOrConstructor?.Body?.Statements.FirstOrDefault(), methodOrConstructor?.Body?.Statements.LastOrDefault());

            foreach (var variable in variables)
            {
                if (!IsUsedVariable(variable, semanticModel, dataFlowAnalysis))
                {
                    ReportDiagnostic(context, variable);
                }
            }
        }


        public bool IsUsedVariable(LocalDeclarationStatementSyntax variable, SemanticModel semanticModel, DataFlowAnalysis dataFlowAnalysis)
        {
            var symbol = semanticModel?.GetDeclaredSymbol(variable?.Declaration?.Variables.First());

            if (dataFlowAnalysis.ReadInside.Contains(symbol) && dataFlowAnalysis.WrittenInside.Contains(symbol))
                return true;

            return false;
        }


        private static SyntaxNodeAnalysisContext ReportDiagnostic(SyntaxNodeAnalysisContext context, LocalDeclarationStatementSyntax variable)
        {
            var diagnostic = Diagnostic.Create(Rule, variable?.GetLocation(), variable?.Declaration?.Variables.First());
            context.ReportDiagnostic(diagnostic);
            return context;
        }

    }
}