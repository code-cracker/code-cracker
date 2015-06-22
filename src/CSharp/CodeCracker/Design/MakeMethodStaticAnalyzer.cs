using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Linq;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeMethodStaticAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use static method";
        internal const string MessageFormat = "Make '{0}' method static.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "If the method is not referencing any instance variable and if you are " +
            "not creating a virtual, abstract, new or partial method, and if it is not a method override, " +
            "your instance method may be changed to a static method.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.MakeMethodStatic.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.MakeMethodStatic));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, AnalyzeMethod, SyntaxKind.MethodDeclaration);

        private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var method = (MethodDeclarationSyntax)context.Node;
            if (method.Modifiers.Any(
                SyntaxKind.StaticKeyword,
                SyntaxKind.PartialKeyword,
                SyntaxKind.VirtualKeyword,
                SyntaxKind.NewKeyword,
                SyntaxKind.AbstractKeyword,
                SyntaxKind.OverrideKeyword)) return;
            if (method.Body == null)
            {
                if (method.ExpressionBody?.Expression == null) return;
                var dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(method.ExpressionBody.Expression);
                if (!dataFlowAnalysis.Succeeded) return;
                if (dataFlowAnalysis.DataFlowsIn.Any(inSymbol => inSymbol.Name == "this")) return;
            }
            else if (method.Body.Statements.Count > 0)
            {
                var dataFlowAnalysis = context.SemanticModel.AnalyzeDataFlow(method.Body);
                if (!dataFlowAnalysis.Succeeded) return;
                if (dataFlowAnalysis.DataFlowsIn.Any(inSymbol => inSymbol.Name == "this")) return;
            }
            var diagnostic = Diagnostic.Create(Rule, method.Identifier.GetLocation(), method.Identifier.ValueText);
            context.ReportDiagnostic(diagnostic);
        }
    }
}