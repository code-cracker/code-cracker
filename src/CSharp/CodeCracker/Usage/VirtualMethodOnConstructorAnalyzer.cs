using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class VirtualMethodOnConstructorAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = "Virtual Method Called On Constructor";
        internal const string Message = "Do not call overridable methods in constructors";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "When a virtual method is called, the actual type that executes the method " +
                                   "is not selected until run time. When a constructor calls a virtual method, " +
                                   "it is possible that the constructor for the instance that invokes the method " +
                                   "has not executed.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.VirtualMethodOnConstructor.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.VirtualMethodOnConstructor));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ConstructorDeclaration);
        }

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var ctor = (ConstructorDeclarationSyntax)context.Node;
            if (ctor.Body == null) return;
            var methodInvocations = ctor.Body.DescendantNodes().OfType<InvocationExpressionSyntax>();
            foreach (var method in methodInvocations)
            {
                var identifier = method.Expression as IdentifierNameSyntax;
                if (identifier == null && !method.ToString().StartsWith("this")) return;
                var methodDeclaration = context.SemanticModel.GetSymbolInfo(method).Symbol;
                if (methodDeclaration == null || !methodDeclaration.IsVirtual) return;
                var diagnostic = Diagnostic.Create(Rule, method.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}