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
    public class DisposableFieldNotDisposedAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Dispose Fields Properly";
        internal const string MessageFormat = "Field {0} should be disposed.";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "This class has a disposable field and is not disposing it.";

        internal static readonly DiagnosticDescriptor RuleForReturned = new DiagnosticDescriptor(
            DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.DisposableFieldNotDisposed_Returned));
        internal static readonly DiagnosticDescriptor RuleForCreated = new DiagnosticDescriptor(
            DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.DisposableFieldNotDisposed_Created));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleForCreated, RuleForReturned);

        public override void Initialize(AnalysisContext context) => context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);

        private static void AnalyzeField(SymbolAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var fieldSymbol = (IFieldSymbol)context.Symbol;
            if (fieldSymbol.IsStatic) return;
            if (!fieldSymbol.Type.AllInterfaces.Any(i => i.ToString() == "System.IDisposable") && fieldSymbol.Type.ToString() != "System.IDisposable") return;
            var fieldSyntaxRef = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            var variableDeclarator = fieldSyntaxRef.GetSyntax() as VariableDeclaratorSyntax;
            if (variableDeclarator == null) return;
            if (ContainingTypeImplementsIDisposableAndCallsItOnTheField(context, fieldSymbol)) return;
            var props = new Dictionary<string, string> { { "variableIdentifier", variableDeclarator.Identifier.ValueText } }.ToImmutableDictionary();
            if (variableDeclarator.Initializer?.Value is InvocationExpressionSyntax)
                context.ReportDiagnostic(Diagnostic.Create(RuleForReturned, variableDeclarator.GetLocation(), props, fieldSymbol.Name));
            else if (variableDeclarator.Initializer?.Value is ObjectCreationExpressionSyntax)
                context.ReportDiagnostic(Diagnostic.Create(RuleForCreated, variableDeclarator.GetLocation(), props, fieldSymbol.Name));
        }

        private static bool ContainingTypeImplementsIDisposableAndCallsItOnTheField(SymbolAnalysisContext context, IFieldSymbol fieldSymbol)
        {
            var containingType = fieldSymbol.ContainingType;
            if (containingType == null) return false;
            var iDisposableInterface = containingType.AllInterfaces.FirstOrDefault(i => i.ToString() == "System.IDisposable");
            if (iDisposableInterface == null) return false;
            var disposableMethod = iDisposableInterface.GetMembers("Dispose").OfType<IMethodSymbol>().First(d => d.Arity == 0);
            var disposeMethodSymbol = containingType.FindImplementationForInterfaceMember(disposableMethod) as IMethodSymbol;
            if (disposeMethodSymbol == null) return false;
            if (disposeMethodSymbol.IsAbstract) return true;
            foreach (MethodDeclarationSyntax disposeMethod in disposeMethodSymbol.DeclaringSyntaxReferences.Select(sr => sr.GetSyntax()))
            {
                if (disposeMethod == null) return false;
                var semanticModel = context.Compilation.GetSemanticModel(disposeMethod.SyntaxTree);
                if (CallsDisposeOnField(fieldSymbol, disposeMethod, semanticModel)) return true;
                var invocations = disposeMethod.DescendantNodes().OfKind<InvocationExpressionSyntax>(SyntaxKind.InvocationExpression);
                foreach (var invocation in invocations)
                {
                    var invocationExpressionSymbol = semanticModel.GetSymbolInfo(invocation.Expression).Symbol;
                    if (invocationExpressionSymbol == null
                        || invocationExpressionSymbol.Kind != SymbolKind.Method
                        || invocationExpressionSymbol.Locations.Any(l => l.Kind != LocationKind.SourceFile)
                        || !invocationExpressionSymbol.ContainingType.Equals(containingType)) continue;
                    foreach (MethodDeclarationSyntax method in invocationExpressionSymbol.DeclaringSyntaxReferences.Select(sr => sr.GetSyntax()))
                        if (CallsDisposeOnField(fieldSymbol, method, semanticModel)) return true;
                }
            }
            return false;
        }

        private static bool CallsDisposeOnField(IFieldSymbol fieldSymbol, MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            var body = (SyntaxNode)method.Body ?? method.ExpressionBody;
            var hasDisposeCall = body.DescendantNodes().OfKind<InvocationExpressionSyntax>(SyntaxKind.InvocationExpression)
                .Any(invocation =>
                {
                    if (invocation?.Expression?.IsKind(SyntaxKind.SimpleMemberAccessExpression) ?? false)
                    {
                        return IsDisposeCallOnField(invocation, fieldSymbol, semanticModel);

                    }

                    if (invocation?.Expression?.IsKind(SyntaxKind.MemberBindingExpression) ?? false)
                    {
                        return IsDisposeWithNullPropagationCallOnField(invocation, fieldSymbol, semanticModel);
                    }

                    return false;
                });
            return hasDisposeCall;
        }

        private static bool IsDisposeCallOnField(InvocationExpressionSyntax expression, IFieldSymbol fieldSymbol, SemanticModel semanticModel)
        {
            var memberAccess = (MemberAccessExpressionSyntax)expression.Expression;
            if (memberAccess?.Name == null) return false;
            if (memberAccess.Name.Identifier.ToString() != "Dispose" || memberAccess.Name.Arity != 0) return false;
            var result = fieldSymbol.Equals(semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol);
            return result;
        }

        private static bool IsDisposeWithNullPropagationCallOnField(InvocationExpressionSyntax expression, IFieldSymbol fieldSymbol, SemanticModel semanticModel)
        {
            var memberBinding = (MemberBindingExpressionSyntax)expression.Expression;
            if (memberBinding?.Name == null) return false;
            if (memberBinding.Name.Identifier.ToString() != "Dispose" || memberBinding.Name.Arity != 0) return false;

            var conditionalAccessExpression = memberBinding.Parent.Parent as ConditionalAccessExpressionSyntax;
            if (conditionalAccessExpression == null) return false;
            var result = fieldSymbol.Equals(semanticModel.GetSymbolInfo(conditionalAccessExpression.Expression).Symbol);
            return result;
        }
    }
}