using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisposablesShouldCallSuppressFinalizeAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Disposables Should Call Suppress Finalize";
        internal const string MessageFormat = "'{0}' should call GC.SuppressFinalize inside the Dispose method.";
        internal const string Category = SupportedCategories.Naming;
        const string Description = "Classes implementing IDisposable should call the GC.SuppressFinalize method in their "
            + "finalize method to avoid any finalizer from being called.\r\n"
            + "This rule should be followed even if the class doesn't have a finalizer as a derived class could have one.";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.DisposablesShouldCallSuppressFinalize));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSymbolAction(AnalyzeAsync, SymbolKind.NamedType);

        private async void AnalyzeAsync(SymbolAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.TypeKind != TypeKind.Class && symbol.TypeKind != TypeKind.Struct) return;
            if (!symbol.Interfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable)) return;
            var disposeMethod = FindDisposeMethod(symbol);
            if (disposeMethod == null) return;
            var syntaxTree = await disposeMethod.DeclaringSyntaxReferences[0]?.GetSyntaxAsync(context.CancellationToken);

            var statements = ((MethodDeclarationSyntax)syntaxTree)?.Body?.Statements.OfType<ExpressionStatementSyntax>();
            if (statements != null)
            {
                foreach (var statement in statements)
                {
                    var invocation = statement.Expression as InvocationExpressionSyntax;
                    var method = invocation?.Expression as MemberAccessExpressionSyntax;
                    if (method != null && ((IdentifierNameSyntax)method.Expression).Identifier.ToString() == "GC" && method.Name.ToString() == "SuppressFinalize")
                        return;
                }
            }
            context.ReportDiagnostic(Diagnostic.Create(Rule, disposeMethod.Locations[0], symbol.Name));
        }

        private static ISymbol FindDisposeMethod(INamedTypeSymbol symbol)
        {
            var methods = symbol.GetMembers().Where(x => x.ToString().Contains($"{x.ContainingType.Name}.Dispose(")).Cast<IMethodSymbol>();
            var disposeWithDisposedParameter = methods.FirstOrDefault(m => m.Parameters.FirstOrDefault()?.Type.SpecialType == SpecialType.System_Boolean);
            return disposeWithDisposedParameter != null ? disposeWithDisposedParameter : methods.FirstOrDefault(m => !m.Parameters.Any());
        }
    }
}