using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemovePrivateMethodNeverUsedAnalyzer : DiagnosticAnalyzer
    {

        internal const string Title = "Unused Method";
        internal const string Message = "Method is not used.";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "When a private method declared  does not used might bring incorrect conclusions.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
             DiagnosticId.RemovePrivateMethodNeverUsed.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: false,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemovePrivateMethodNeverUsed));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            if (!methodDeclaration.Modifiers.Any(a => a.ValueText == SyntaxFactory.Token(SyntaxKind.PrivateKeyword).ValueText)) return;
            if (IsMethodUsed(methodDeclaration)) return;
            if (methodDeclaration.Modifiers.Any(SyntaxKind.ExternKeyword)) return;
            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private bool IsMethodUsed(MethodDeclarationSyntax methodTarget)
        {
            var typeDeclaration = methodTarget.Parent as TypeDeclarationSyntax;
            if (typeDeclaration == null) return true;

            var hasIdentifier = typeDeclaration.DescendantNodes()?.OfType<IdentifierNameSyntax>();
            if (hasIdentifier == null || !hasIdentifier.Any()) return false;
            return hasIdentifier.Any(a => a != null && a.Identifier.ValueText.Equals(methodTarget?.Identifier.ValueText));
        }
    }
}
