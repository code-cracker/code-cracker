using System.Collections.Generic;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;

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
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyze, SyntaxKind.MethodDeclaration);

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;

            var semanticModel = context.SemanticModel;
            var method = (MethodDeclarationSyntax)context.Node;

            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            var isImplicitDispose = methodSymbol.ToString().Contains($"{methodSymbol.ContainingType.Name}.Dispose(");
            var isExplicitDispose =
                methodSymbol.ExplicitInterfaceImplementations.Any(i => i.ToString() == "System.IDisposable.Dispose()");

            if (!isImplicitDispose && !isExplicitDispose)
                return;

            if (methodSymbol.Parameters != null && methodSymbol.Parameters.Length > 0)
                return;

            var symbol = methodSymbol.ContainingType;
            if (symbol.TypeKind != TypeKind.Class) return;
            if (!symbol.Interfaces.Any(i => i.SpecialType == SpecialType.System_IDisposable)) return;
            if (symbol.IsSealed && !ContainsUserDefinedFinalizer(symbol)) return;
            if (!ContainsNonPrivateConstructors(symbol)) return;

            var statements = method.Body?.Statements.OfType<ExpressionStatementSyntax>();
            if (statements != null)
            {
                if (StatementsContainSuppressFinalize(statements, semanticModel)) return;
            }

            var tryStatements = method.Body?.Statements.OfType<TryStatementSyntax>();
            if (tryStatements != null)
            {
                foreach (var tryStatement in tryStatements)
                {
                    var finallySyntax = tryStatement.Finally;
                    var finallyExpressionsStatements = finallySyntax.Block.Statements.OfType<ExpressionStatementSyntax>();

                    if (finallyExpressionsStatements != null)
                    {
                        if (StatementsContainSuppressFinalize(finallyExpressionsStatements, semanticModel)) return;
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, methodSymbol.Locations[0], symbol.Name));
        }

        private static bool StatementsContainSuppressFinalize(IEnumerable<ExpressionStatementSyntax> statements, SemanticModel semanticModel)
        {
            foreach (var statement in statements)
            {
                var invocation = statement.Expression as InvocationExpressionSyntax;
                var suppress = invocation?.Expression as MemberAccessExpressionSyntax;

                if (suppress?.Name.ToString() != "SuppressFinalize")
                    continue;

                var containingType = semanticModel.GetSymbolInfo(suppress.Expression).Symbol as INamedTypeSymbol;
                if (containingType?.ContainingNamespace.Name != "System")
                    continue;

                if (containingType.Name == "GC")
                    return true;
            }
            return false;
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

        private static bool IsNestedPrivateType(ISymbol symbol)
        {
            if (symbol == null)
                return false;

            if (symbol.DeclaredAccessibility == Accessibility.Private && symbol.ContainingType != null)
                return true;

            return IsNestedPrivateType(symbol.ContainingType);
        }
    }
}