using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Reliability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseConfigureAwaitFalseAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use ConfigureAwait(false) on awaited task.";
        internal const string MessageFormat = "Consider using ConfigureAwait(false) on the awaited task.";
        internal const string Category = SupportedCategories.Reliability;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.UseConfigureAwaitFalse.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.UseConfigureAwaitFalse));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.AwaitExpression);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var awaitExpression = (AwaitExpressionSyntax)context.Node;
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