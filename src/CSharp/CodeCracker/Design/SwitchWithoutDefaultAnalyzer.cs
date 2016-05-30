using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;


namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SwitchWithoutDefaultAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Your Switch maybe include default clause";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Design;
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.SwitchCaseWithoutDefault.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.SwitchCaseWithoutDefault));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.SwitchStatement);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            // checks if the compile error CS0151 it has fired.If Yes, don't report the diagnostic
            var diagnostics = from dia in context.SemanticModel.GetDiagnostics()
                              where dia.Id == "CS0151"
                              select dia;
            if (diagnostics.ToList().Count == 0)
            {
                var defaultSwitchSyntaxe = (SwitchStatementSyntax)context.Node;
                if (defaultSwitchSyntaxe.DescendantNodes().Where(n => n.Kind() == SyntaxKind.DefaultSwitchLabel).ToList().Count == 0)
                {
                    var variables = ((LocalDeclarationStatementSyntax)defaultSwitchSyntaxe.GetPreviousStatement()).Declaration.Variables.Last();
                    if (variables != null)
                    {
                        // if boolean variables, don't report the diagnostic
                        var kindVariable = ((VariableDeclaratorSyntax)variables).Initializer.Value.Kind();
                        if ((kindVariable != SyntaxKind.FalseLiteralExpression) && (kindVariable != SyntaxKind.TrueLiteralExpression))
                        {
                            var diagnostic = Diagnostic.Create(Rule, defaultSwitchSyntaxe.GetLocation(), "Consider put an default clause in Switch.");
                            context.ReportDiagnostic(diagnostic);
                        }
                    }
                }
            }
        }
    }
}
