using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class TaskNameAsyncAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Asynchronous method can be terminated with the 'Async' keyword.";
        internal const string MessageFormat = "Change method name to {0}";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Asynchronous method can be terminated with the 'Async' keyword.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.TaskNameAsync.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.TaskNameAsync));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var method = (MethodDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            if (method.IsImplementingInterface(semanticModel)) return;
            if (method.Identifier.ToString().EndsWith("Async")) return;
            if (method.Modifiers.Any(SyntaxKind.NewKeyword, SyntaxKind.OverrideKeyword)) return;
            var errorMessage = method.Identifier.ToString() + "Async";
            var diag = Diagnostic.Create(Rule, method.Identifier.GetLocation(), errorMessage);
            if (method.Modifiers.Any(SyntaxKind.AsyncKeyword))
            {
                context.ReportDiagnostic(diag);
                return;
            }
            var returnType = semanticModel.GetSymbolInfo(method.ReturnType).Symbol as INamedTypeSymbol;
            if (returnType == null) return;
            if (returnType.ToString() != "System.Threading.Tasks.Task" &&
                (!returnType.IsGenericType || returnType.ConstructedFrom.ToString() != "System.Threading.Tasks.Task<TResult>"))
                return;
            context.ReportDiagnostic(diag);
        }
    }
}
