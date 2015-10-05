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
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.DisposablesShouldCallSuppressFinalize));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

        private static void Analyze(SymbolAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var symbol = (INamedTypeSymbol)context.Symbol;
            if (symbol.TypeKind != TypeKind.Class) return;
            if (!symbol.Interfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable)) return;
            if (symbol.IsSealed && !ContainsUserDefinedFinalizer(symbol)) return;
            if (!ContainsNonPrivateConstructors(symbol)) return;
            var disposeMethod = FindDisposeMethod(symbol);
            if (disposeMethod == null) return;
            var syntaxTree = disposeMethod.DeclaringSyntaxReferences[0]?.GetSyntax();

            var statements = ((MethodDeclarationSyntax)syntaxTree)?.Body?.Statements.OfType<ExpressionStatementSyntax>();
            if (statements != null)
            {
                foreach (var statement in statements)
                {
                    var invocation = statement.Expression as InvocationExpressionSyntax;
                    var method = invocation?.Expression as MemberAccessExpressionSyntax;
                    var identifierSyntax = method?.Expression as IdentifierNameSyntax;
                    if (identifierSyntax != null && identifierSyntax.Identifier.ToString() == "GC" && method.Name.ToString() == "SuppressFinalize")
                        return;
                }
            }
            context.ReportDiagnostic(Diagnostic.Create(Rule, disposeMethod.Locations[0], symbol.Name));
        }

        private static ISymbol FindDisposeMethod(INamedTypeSymbol symbol)
        {
            return symbol.GetMembers().Where(x => x.ToString().Contains($"{x.ContainingType.Name}.Dispose(")).Cast<IMethodSymbol>()
                .FirstOrDefault(m => m.Parameters == null || m.Parameters.Length == 0);
        }

        public static bool ContainsUserDefinedFinalizer(INamedTypeSymbol symbol)
        {
            return symbol.GetMembers()
                .Any(x => x.ToString().Contains($".~{x.ContainingType.Name}("));
        }

        public static bool ContainsNonPrivateConstructors(INamedTypeSymbol symbol)
        {
            if (IsNestedPrivateType(symbol))
                return false;

            return symbol.GetMembers()
                .Any(m => m.MetadataName == ".ctor" && m.DeclaredAccessibility != Accessibility.Private);
        }

        private static bool IsNestedPrivateType(INamedTypeSymbol symbol)
        {
            if (symbol == null)
                return false;

            if (symbol.DeclaredAccessibility == Accessibility.Private && symbol.ContainingType != null)
                return true;

            return IsNestedPrivateType(symbol.ContainingType);
        }
    }
}