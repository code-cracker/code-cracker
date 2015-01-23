using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;

namespace CodeCracker.Performance
{
    public class SealedAttributeAnalyzer : CSharpAnalyzer
    {
        public SealedAttributeAnalyzer() : base(new DiagnosticDescriptorInfo
        {
            Id = DiagnosticId.SealedAttributeAnalyzer,
            Title = "Unsealed Attribute",
            Message = "Mark '{0}' as sealed.",
            Category = SupportedCategories.Performance,
            Severity = DiagnosticSeverity.Warning,
            Description = "Framework methods that retrieve attributes by default search the whole "
                + "inheritence hierarchy of the attribute class. "
                + "Marking the type as sealed eliminate this search and can improve performance"
        }) { }

        public override void Initialize(AnalysisContext context) => context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);

        private void Analyze(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (type.TypeKind != TypeKind.Class) return;

            if (!IsAttribute(type)) return;

            if (type.IsAbstract || type.IsSealed) return;

            ReportDiagnostic(context, type.Locations[0], type.Name);
        }

        public static bool IsAttribute(ITypeSymbol symbol)
        {
            var @base = symbol.BaseType;
            var attributeName = typeof(Attribute).Name;

            while (@base != null)
            {
                if (@base.Name == attributeName)
                    return true;

                @base = @base.BaseType;
            }

            return false;
        }
    }
}