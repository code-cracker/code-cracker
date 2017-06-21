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
        const string Description = "Unused private methods can be safely removed as they are unnecessary.";

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
            if (IsMethodAttributeAnException(methodDeclaration)) return;
            if (IsMethodUsed(methodDeclaration, context.SemanticModel)) return;
            if (IsMainMethodEntryPoint(methodDeclaration, context.SemanticModel)) return;
            if (methodDeclaration.Modifiers.Any(SyntaxKind.ExternKeyword)) return;
            if (IsWinformsPropertyDefaultValueDefinitionMethod(methodDeclaration, context.SemanticModel)) return;
            var props = new Dictionary<string, string> { { "identifier", methodDeclaration.Identifier.Text } }.ToImmutableDictionary();
            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation(), props);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsMethodAttributeAnException(MethodDeclarationSyntax methodDeclaration)
        {
            if (methodDeclaration == null) return false;

            foreach (var attributeList in methodDeclaration.AttributeLists)
            {
                foreach (var attribute in attributeList.Attributes)
                {
                    var identifierName = attribute.Name as IdentifierNameSyntax;
                    string nameText = null;
                    if (identifierName != null)
                    {
                        nameText = identifierName?.Identifier.Text;
                    }
                    else
                    {
                        var qualifiedName = attribute.Name as QualifiedNameSyntax;
                        if (qualifiedName != null)
                            nameText = qualifiedName.Right?.Identifier.Text;
                    }
                    if (nameText == null) continue;
                    if (IsExcludedAttributeName(nameText)) return true;
                }
            }
            return false;
        }

        // Some Attributes make it valid to have an unused private Method, this is a list of them
        private static readonly string[] excludedAttributeNames = { "Fact", "ContractInvariantMethod", "DataMember" };

        private static bool IsExcludedAttributeName(string attributeName) =>
            excludedAttributeNames.Contains(attributeName);

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

        // see https://msdn.microsoft.com/en-us/library/53b8022e(v=vs.110).aspx
        private static bool IsWinformsPropertyDefaultValueDefinitionMethod(MethodDeclarationSyntax methodTarget, SemanticModel semanticModel)
        {
            var propertyName = GetPropertyNameForWinformDefaultValueMethods(methodTarget, semanticModel);
            if (string.IsNullOrWhiteSpace(propertyName)) return false;
            if (!ExistsProperty(propertyName, methodTarget, semanticModel)) return false;
            return true;
        }

        private static string GetPropertyNameForWinformDefaultValueMethods(MethodDeclarationSyntax methodTarget, SemanticModel semanticModel) =>
            GetPropertyNameForMethodWithSignature(methodTarget, semanticModel, "Reset", "Void") ??
            GetPropertyNameForMethodWithSignature(methodTarget, semanticModel, "ShouldSerialize", "Boolean");

        private static string GetPropertyNameForMethodWithSignature(MethodDeclarationSyntax methodTarget, SemanticModel semanticModel, string startsWith, string returnType)
        {
            var methodName = methodTarget.Identifier.Text;
            if (methodName.StartsWith(startsWith))
                if (methodTarget.ParameterList.Parameters.Count == 0)
                {
                    var returnTypeInfo = semanticModel.GetTypeInfo(methodTarget.ReturnType).Type;
                    if (returnTypeInfo.Name.Equals(returnType, StringComparison.OrdinalIgnoreCase))
                        return methodName.Substring(startsWith.Length); ;
                }
            return null;
        }

        private static bool ExistsProperty(string propertyName, SyntaxNode nodeInType, SemanticModel semanticModel)
        {
            var typeDeclaration = nodeInType.AncestorsAndSelf().OfType<TypeDeclarationSyntax>().FirstOrDefault();
            if (typeDeclaration == null) return false;
            var propertyDeclarations = typeDeclaration.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            return propertyDeclarations.Any(pd => pd.Identifier.Text == propertyName);
        }
    }
}
