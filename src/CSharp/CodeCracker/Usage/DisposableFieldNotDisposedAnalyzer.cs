using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisposableFieldNotDisposedAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticIdReturned = "CC0032";
        internal const string Title = "Use object initializer";
        internal const string MessageFormat = "Field {0} should be disposed.";
        internal const string Category = SupportedCategories.Usage;
        public const string DiagnosticIdCreated = "CC0033";
        const string Description = "This class has a disposable field and is not disposing it.";

        internal static DiagnosticDescriptor RuleForReturned = new DiagnosticDescriptor(
            DiagnosticIdReturned,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticIdReturned));
        internal static DiagnosticDescriptor RuleForCreated = new DiagnosticDescriptor(
            DiagnosticIdCreated,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticIdCreated));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(RuleForCreated, RuleForReturned); } }

        public override void Initialize(AnalysisContext context) => context.RegisterSymbolAction(AnalyzeField, SymbolKind.Field);

        private void AnalyzeField(SymbolAnalysisContext context)
        {
            var fieldSymbol = (IFieldSymbol)context.Symbol;
            if (!fieldSymbol.Type.AllInterfaces.Any(i => i.ToString() == "System.IDisposable") && fieldSymbol.Type.ToString() != "System.IDisposable") return;
            var fieldSyntaxRef = fieldSymbol.DeclaringSyntaxReferences.FirstOrDefault();
            var variableDeclarator = fieldSyntaxRef.GetSyntax() as VariableDeclaratorSyntax;
            if (variableDeclarator == null) return;
            if (ContainingTypeImplementsIDisposableAndCallsItOnTheField(context, fieldSymbol, fieldSymbol.ContainingType)) return;
            if (variableDeclarator.Initializer?.Value is InvocationExpressionSyntax)
                context.ReportDiagnostic(Diagnostic.Create(RuleForReturned, variableDeclarator.GetLocation(), fieldSymbol.Name));
            else if (variableDeclarator.Initializer?.Value is ObjectCreationExpressionSyntax)
                context.ReportDiagnostic(Diagnostic.Create(RuleForCreated, variableDeclarator.GetLocation(), fieldSymbol.Name));
        }

        private bool ContainingTypeImplementsIDisposableAndCallsItOnTheField(SymbolAnalysisContext context, IFieldSymbol fieldSymbol, INamedTypeSymbol typeSymbol)
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