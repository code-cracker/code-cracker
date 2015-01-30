using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.Reliability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseConfigureAwaitFalseAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0070";
        internal const string Title = "Use ConfigureAwait(false) on awaited task.";
        internal const string MessageFormat = "Consider using ConfigureAwait(false) on the awaited task.";
        internal const string Category = SupportedCategories.Reliability;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AwaitExpression);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var awaitExpression = (AwaitExpressionSyntax) context.Node;
            var awaitedExpression = awaitExpression.Expression;
            if (!IsTask(awaitedExpression, context))
                return;

            var diagnostic = Diagnostic.Create(Rule, awaitExpression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsTask(ExpressionSyntax expression, SyntaxNodeAnalysisContext context)
        {
            var type = context.SemanticModel.GetTypeInfo(expression).Type as INamedTypeSymbol;
            if (type == null)
                return false;
            INamedTypeSymbol taskType;

            if (type.IsGenericType)
            {
                type = type.ConstructedFrom;
                taskType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task`1");
            }
            else
            {
                taskType = context.SemanticModel.Compilation.GetTypeByMetadataName("System.Threading.Tasks.Task");
            }
            return type.Equals(taskType);
        }
    }
}
