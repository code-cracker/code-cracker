using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace CodeCracker.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TaskNameAsyncAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0061";
        internal const string Title = "Async method can be terminating with 'Async' name.";
        internal const string MessageFormat = "Change method name to {0}";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Async method can be terminating with 'Async' name.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        }

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (MethodDeclarationSyntax)context.Node;
            if (invocationExpression.Identifier.ToString().EndsWith("Async")) return;
            
            var returnType = context.SemanticModel.GetSymbolInfo(invocationExpression.ReturnType).Symbol?.ToString();
            if (returnType == null) return;

            if (invocationExpression.Modifiers.Any(SyntaxKind.AsyncKeyword) || returnType.StartsWith("System.Threading.Tasks.Task"))
            {
                var errorMessage = invocationExpression.Identifier.ToString() + "Async";
                var diag = Diagnostic.Create(Rule, invocationExpression.GetLocation(), errorMessage);
                context.ReportDiagnostic(diag);
            }
        }
    }
}