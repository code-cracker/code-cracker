using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveAsyncFromMethodAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Remove Async termination when method is not asynchronous.";
        internal const string MessageFormat = "Consider remove 'Async' from method {0}.";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Remove Async termination when method is not asynchronous.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RemoveAsyncFromMethod.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveAsyncFromMethod));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);

        private static void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var method = context.Node as MethodDeclarationSyntax;
            if (!method.Identifier.Text.EndsWith("Async")) return;
            if (method.Modifiers.Any(m => m.Text == "async")) return;

            var returnType = context.SemanticModel.GetSymbolInfo(method.ReturnType).Symbol as INamedTypeSymbol;
            if (returnType != null)
            {
                if (returnType.ToString() == "System.Threading.Tasks.Task" ||
                    (returnType.IsGenericType && returnType.ConstructedFrom.ToString() == "System.Threading.Tasks.Task<TResult>"))
                    return;
            }
            var diagnostic = Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.Text);
            context.ReportDiagnostic(diagnostic);
        }

    }
}