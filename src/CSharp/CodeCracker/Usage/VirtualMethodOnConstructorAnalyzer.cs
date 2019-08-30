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
            SeverityConfigurations.Current[DiagnosticId.VirtualMethodOnConstructor],
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
            var semanticModel = context.SemanticModel;
            foreach (var invocation in methodInvocations)
            {
                var identifier = invocation.Expression as IdentifierNameSyntax;
                if (identifier == null)
                {
                    if (!invocation.ToString().StartsWith("this.")) return;
                }
                else
                {
                    if (semanticModel.GetSymbolInfo(identifier).Symbol is IParameterSymbol) return;
                }
                var methodSymbol = semanticModel.GetSymbolInfo(invocation).Symbol;
                if (methodSymbol == null || !methodSymbol.IsVirtual) return;
                var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}