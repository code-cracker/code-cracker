using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SealedAttributeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.SealedAttributeAnalyzer";
        internal const string Title = "Unsealed Attribute";
        internal const string MessageFormat = "Mark '{0}' as sealed.";
        internal const string Category = "Performance";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
        }

        private void Analyze(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (type.TypeKind != TypeKind.Class) return;

            if (type.BaseType.ToString() != "System.Attribute") return;

            if (type.IsAbstract || type.IsSealed) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.Name));
        }
    }
}