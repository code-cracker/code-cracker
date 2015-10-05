using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnusedParametersAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Unused parameters";
        internal const string Message = "Parameter '{0}' is not used.";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "When a method declares a parameter and does not use it might bring incorrect conclusions for anyone reading the code and also demands the parameter when the method is called, unnecessarily.\r\n"
            + "You should delete the parameter in such cases.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.UnusedParameters.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            customTags: WellKnownDiagnosticTags.Unnecessary,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.UnusedParameters));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.MethodDeclaration, SyntaxKind.ConstructorDeclaration);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var methodOrConstructor = context.Node as BaseMethodDeclarationSyntax;
            if (methodOrConstructor == null) return;
            var semanticModel = context.SemanticModel;
            if (!IsCandidateForRemoval(methodOrConstructor, semanticModel)) return;
            var parameters = methodOrConstructor.ParameterList.Parameters.ToDictionary(p => p, p => semanticModel.GetDeclaredSymbol(p));
            var ctor = methodOrConstructor as ConstructorDeclarationSyntax;
            if (ctor?.Initializer != null)
            {
                var symbolsTouched = new List<ISymbol>();
                foreach (var arg in ctor.Initializer.ArgumentList.Arguments)
                {
                    var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(arg.Expression);
                    if (!dataFlowAnalysis.Succeeded) continue;
                    symbolsTouched.AddRange(dataFlowAnalysis.ReadInside);
                    symbolsTouched.AddRange(dataFlowAnalysis.WrittenInside);
                }
                var parametersToRemove = parameters.Where(p => symbolsTouched.Contains(p.Value)).ToList();
                foreach (var parameter in parametersToRemove)
                    parameters.Remove(parameter.Key);
            }
            if (methodOrConstructor.Body.Statements.Any())
            {
                var dataFlowAnalysis = semanticModel.AnalyzeDataFlow(methodOrConstructor.Body.Statements.First(), methodOrConstructor.Body.Statements.Last());
                if (!dataFlowAnalysis.Succeeded) return;
                foreach (var parameter in parameters)
                {
                    var parameterSymbol = parameter.Value;
                    if (parameterSymbol == null) continue;
                    if (!dataFlowAnalysis.ReadInside.Contains(parameterSymbol) && !dataFlowAnalysis.WrittenInside.Contains(parameterSymbol))
                        context = ReportDiagnostic(context, parameter.Key);
                }
            }
            else
            {
                foreach (var parameter in parameters.Keys)
                    context = ReportDiagnostic(context, parameter);
            }
        }

        private static bool IsCandidateForRemoval(BaseMethodDeclarationSyntax methodOrConstructor, SemanticModel semanticModel)
        {
            if (methodOrConstructor.Modifiers.Any(m => m.ValueText == "partial" || m.ValueText == "override")
                || !methodOrConstructor.ParameterList.Parameters.Any()
                || methodOrConstructor.Body == null)
                return false;
            var method = methodOrConstructor as MethodDeclarationSyntax;
            if (method != null)
            {
                if (method.ExplicitInterfaceSpecifier != null) return false;
                var methodSymbol = semanticModel.GetDeclaredSymbol(method);
                if (methodSymbol == null) return false;
                var typeSymbol = methodSymbol.ContainingType;
                if (typeSymbol.AllInterfaces.SelectMany(i => i.GetMembers())
                    .Any(member => methodSymbol.Equals(typeSymbol.FindImplementationForInterfaceMember(member))))
                    return false;
                if (IsEventHandlerLike(method, semanticModel)) return false;
            }
            else
            {
                var constructor = methodOrConstructor as ConstructorDeclarationSyntax;
                if (constructor != null)
                {
                    if (IsSerializationConstructor(constructor, semanticModel)) return false;
                }
                else
                {
                    return false;
                }
            }
            return true;
        }

        private static bool IsSerializationConstructor(ConstructorDeclarationSyntax constructor, SemanticModel semanticModel)
        {
            if (constructor.ParameterList.Parameters.Count != 2) return false;
            var constructorSymbol = semanticModel.GetDeclaredSymbol(constructor);
            var typeSymbol = constructorSymbol?.ContainingType;
            if (!typeSymbol?.AllInterfaces.Any(i => i.ToString() == "System.Runtime.Serialization.ISerializable") ?? true) return false;
            if (!typeSymbol.GetAttributes().Any(a => a.AttributeClass.ToString() == "System.SerializableAttribute")) return false;
            var serializationInfoType = semanticModel.GetTypeInfo(constructor.ParameterList.Parameters[0].Type).Type as INamedTypeSymbol;
            if (serializationInfoType == null) return false;
            if (!serializationInfoType.AllBaseTypesAndSelf().Any(type => type.ToString() == "System.Runtime.Serialization.SerializationInfo"))
                return false;
            var streamingContextType = semanticModel.GetTypeInfo(constructor.ParameterList.Parameters[1].Type).Type as INamedTypeSymbol;
            if (streamingContextType == null) return false;
            return streamingContextType.AllBaseTypesAndSelf().Any(type => type.ToString() == "System.Runtime.Serialization.StreamingContext");
        }

        private static bool IsEventHandlerLike(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            if (method.ParameterList.Parameters.Count != 2
                || method.ReturnType.ToString() != "void")
                return false;
            var senderType = semanticModel.GetTypeInfo(method.ParameterList.Parameters[0].Type).Type;
            if (senderType.SpecialType != SpecialType.System_Object) return false;
            var eventArgsType = semanticModel.GetTypeInfo(method.ParameterList.Parameters[1].Type).Type as INamedTypeSymbol;
            if (eventArgsType == null) return false;
            return eventArgsType.AllBaseTypesAndSelf().Any(type => type.ToString() == "System.EventArgs");
        }

        private static SyntaxNodeAnalysisContext ReportDiagnostic(SyntaxNodeAnalysisContext context, ParameterSyntax parameter)
        {
            var props = new Dictionary<string, string> { { "identifier", parameter.Identifier.Text } }.ToImmutableDictionary();
            var diagnostic = Diagnostic.Create(Rule, parameter.GetLocation(), props, parameter.Identifier.ValueText);
            context.ReportDiagnostic(diagnostic);
            return context;
        }
    }
}