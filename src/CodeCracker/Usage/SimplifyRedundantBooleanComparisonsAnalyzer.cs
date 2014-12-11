using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SimplifyRedundantBooleanComparisonsAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0049";
        internal const string Title = "Simplify expression";
        internal const string MessageFormat = "You can remove this comparison.";
        internal const string Category = SupportedCategories.Usage;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.EqualsExpression, SyntaxKind.NotEqualsExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var comparison = (BinaryExpressionSyntax)context.Node;

            var semanticModel = context.SemanticModel;

            var right = context.SemanticModel.GetConstantValue(comparison.Right);
            if (!right.HasValue) return;

            if (!(right.Value is bool))
            {
                return;
            }

            var diagnostic = Diagnostic.Create(Rule, comparison.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}
