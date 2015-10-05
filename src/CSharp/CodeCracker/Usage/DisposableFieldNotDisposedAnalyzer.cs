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
            if (!fieldSymbol.Type.AllInterfaces.Any(i => i.ToString() == "System.IDisposable") && fieldSymbol.Type.ToString() != "System.IDisposable") return;
            var fieldSyntaxRef = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            var variableDeclarator = fieldSyntaxRef.GetSyntax() as VariableDeclaratorSyntax;
            if (variableDeclarator == null) return;
            if (ContainingTypeImplementsIDisposableAndCallsItOnTheField(context, fieldSymbol, fieldSymbol.ContainingType)) return;
            var props = new Dictionary<string, string> { { "variableIdentifier", variableDeclarator.Identifier.ValueText } }.ToImmutableDictionary();
            if (variableDeclarator.Initializer?.Value is InvocationExpressionSyntax)
                context.ReportDiagnostic(Diagnostic.Create(RuleForReturned, variableDeclarator.GetLocation(), props, fieldSymbol.Name));
            else if (variableDeclarator.Initializer?.Value is ObjectCreationExpressionSyntax)
                context.ReportDiagnostic(Diagnostic.Create(RuleForCreated, variableDeclarator.GetLocation(), props, fieldSymbol.Name));
        }

        private static bool ContainingTypeImplementsIDisposableAndCallsItOnTheField(SymbolAnalysisContext context, IFieldSymbol fieldSymbol, INamedTypeSymbol typeSymbol)
        {
            if (typeSymbol == null) return false;
            var iDisposableInterface = typeSymbol.AllInterfaces.FirstOrDefault(i => i.ToString() == "System.IDisposable");
            if (iDisposableInterface != null)
            {
                var disposableMethod = iDisposableInterface.GetMembers("Dispose").OfType<IMethodSymbol>().First(d => d.Arity == 0);
                var disposeMethodSymbol = typeSymbol.FindImplementationForInterfaceMember(disposableMethod) as IMethodSymbol;
                if (disposeMethodSymbol != null)
                {
                    var disposeMethod = (MethodDeclarationSyntax)disposeMethodSymbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                    if (disposeMethod == null) return false;
                    if (disposeMethod.Modifiers.Any(SyntaxKind.AbstractKeyword)) return true;
                    var typeDeclaration = (TypeDeclarationSyntax)typeSymbol.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax();
                    var semanticModel = context.Compilation.GetSemanticModel(typeDeclaration.SyntaxTree);
                    if (CallsDisposeOnField(fieldSymbol, disposeMethod, semanticModel)) return true;
                }
            }
            return false;
        }

        private static bool CallsDisposeOnField(IFieldSymbol fieldSymbol, MethodDeclarationSyntax disposeMethod, SemanticModel semanticModel)
        {
            var hasDisposeCall = disposeMethod.Body.Statements.OfType<ExpressionStatementSyntax>()
                .Any(exp =>
                {
                    var invocation = exp.Expression as InvocationExpressionSyntax;
                    if (!invocation?.Expression?.IsKind(SyntaxKind.SimpleMemberAccessExpression) ?? true) return false;
                    var memberAccess = (MemberAccessExpressionSyntax)invocation.Expression;
                    if (memberAccess.Name.Identifier.ToString() != "Dispose" || memberAccess.Name.Arity != 0) return false;
                    var memberAccessIdentificer = memberAccess.Expression as IdentifierNameSyntax;
                    if (memberAccessIdentificer == null) return false;
                    return fieldSymbol.Equals(semanticModel.GetSymbolInfo(memberAccessIdentificer).Symbol);
                });
            return hasDisposeCall;
        }
    }
}