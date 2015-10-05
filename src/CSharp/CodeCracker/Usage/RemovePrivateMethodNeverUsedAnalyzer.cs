using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
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

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
             DiagnosticId.RemovePrivateMethodNeverUsed.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemovePrivateMethodNeverUsed));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var methodDeclaration = (MethodDeclarationSyntax)context.Node;
            if (methodDeclaration.ExplicitInterfaceSpecifier != null) return;
            var methodSymbol = context.SemanticModel.GetDeclaredSymbol(methodDeclaration);
            if (methodSymbol.DeclaredAccessibility != Accessibility.Private) return;
            if (IsMethodUsed(methodDeclaration, context.SemanticModel)) return;
            if (IsMainMethodEntryPoint(methodDeclaration, context.SemanticModel)) return;
            if (methodDeclaration.Modifiers.Any(SyntaxKind.ExternKeyword)) return;
            var props = new Dictionary<string, string> { { "identifier", methodDeclaration.Identifier.Text } }.ToImmutableDictionary();
            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation(), props);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsMethodUsed(MethodDeclarationSyntax methodTarget, SemanticModel semanticModel)
        {
            var typeDeclaration = methodTarget.Parent as TypeDeclarationSyntax;
            if (typeDeclaration == null) return true;

            if (!typeDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
                return IsMethodUsed(methodTarget, typeDeclaration);

            var symbol = semanticModel.GetDeclaredSymbol(typeDeclaration);

            return
                symbol == null ||
                symbol.DeclaringSyntaxReferences.Any(reference => IsMethodUsed(methodTarget, reference.GetSyntax()));
        }

        private static bool IsMethodUsed(MethodDeclarationSyntax methodTarget, SyntaxNode typeDeclaration)
        {
            var descendents = typeDeclaration.DescendantNodes();
            var hasIdentifier = descendents.OfType<IdentifierNameSyntax>();
            if (hasIdentifier.Any(a => a != null && a.Identifier.ValueText.Equals(methodTarget.Identifier.ValueText)))
                return true;
            var genericNames = descendents.OfType<GenericNameSyntax>();
            return genericNames.Any(n => n != null && n.Identifier.ValueText.Equals(methodTarget.Identifier.ValueText));
        }

        private static bool IsMainMethodEntryPoint(MethodDeclarationSyntax methodTarget, SemanticModel semanticModel)
        {
            if (!methodTarget.Identifier.Text.Equals("Main", StringComparison.Ordinal)) return false;
            if (!methodTarget.Modifiers.Any(SyntaxKind.StaticKeyword)) return false;

            var returnType = semanticModel.GetTypeInfo(methodTarget.ReturnType).Type;
            if (returnType == null) return false;
            if (!returnType.Name.Equals("Void", StringComparison.OrdinalIgnoreCase) &&
                !returnType.Name.Equals("Int32", StringComparison.OrdinalIgnoreCase))
                return false;

            var parameters = methodTarget.ParameterList.Parameters;
            if (parameters.Count > 1) return false;
            if (parameters.Count == 0) return true;
            var parameterType = semanticModel.GetTypeInfo(parameters.First().Type).Type;
            if (!parameterType.OriginalDefinition.ToString().Equals("String[]", StringComparison.OrdinalIgnoreCase)) return false;
            return true;
        }
    }
}