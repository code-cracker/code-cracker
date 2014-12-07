using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class DisposablesShouldCallSuppressFinalizeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0029";
        internal const string Title = "Disposables Should Call Suppress Finalize";
        internal const string MessageFormat = "'{0}' should call GC.SuppressFinalize inside the Dispose method.";
        internal const string Category = SupportedCategories.Naming;
        const string Description = "Classes implementing IDisposable should call the GC.SuppressFinalize method in their "
            + "finalize method to avoid any finalizer from being called.\r\n"
            + "This rule should be followed even if the class doesn't have a finalizer as a derived class could have one.";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description:Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
        }

        private async void Analyze(SymbolAnalysisContext context)
        {
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

                    if (invocation != null)
                    {
                        var method = invocation.Expression as MemberAccessExpressionSyntax;

                        if (method != null)
                        {
                            if (((IdentifierNameSyntax)method.Expression).Identifier.ToString() == "GC" && method.Name.ToString() == "SuppressFinalize")
                            {
                                return;
                            }
                        }
                    }
                }
            }

            context.ReportDiagnostic(Diagnostic.Create(Rule, disposeMethod.Locations[0], symbol.Name));
        }

        private static ISymbol FindDisposeMethod(INamedTypeSymbol symbol)
        {
            var methods = symbol.GetMembers("Dispose").Cast<IMethodSymbol>();
            var disposeWithDisposedParameter = methods.SingleOrDefault(m => m.Parameters.SingleOrDefault()?.Type.SpecialType == SpecialType.System_Boolean);

            return disposeWithDisposedParameter != null ? disposeWithDisposedParameter : methods.SingleOrDefault(m => !m.Parameters.Any());
        }
    }
}