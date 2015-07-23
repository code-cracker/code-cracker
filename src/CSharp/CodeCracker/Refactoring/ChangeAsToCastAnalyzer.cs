using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ChangeAsToCastAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Toggle between safe cast ('as') and direct cast.";
        internal const string MessageFormat = "Toggle between safe cast ('as') and direct cast.";
        internal const string Category = SupportedCategories.Refactoring;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ChangeAsToCast.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ChangeAsToCast));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AsExpression);
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.CastExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            // Exclude cases like "obj as whatever", where "whatever" is not a type
            var right = (context.Node as BinaryExpressionSyntax)?.Right;
            if (right != null && !(right is TypeSyntax))
                return;

            // Exclude casts to value type
            if (context.Node.Kind() == SyntaxKind.CastExpression)
            {
                var type = context.SemanticModel.GetTypeInfo(context.Node).Type;
                if (type?.IsValueType == true)
                    return;
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, context.Node.GetLocation()));
        }
    }
}
