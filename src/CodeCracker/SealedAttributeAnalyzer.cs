using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SealedAttributeAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0023";
        internal const string Title = "Unsealed Attribute";
        internal const string MessageFormat = "Mark '{0}' as sealed.";
        internal const string Category = "Performance";
        const string Description = "Framework methods that retrieve attributes by default search the whole "
            + "inheritence hierarchy of the attribute class. "
            + "Marking the type as sealed eliminate this search and can improve performance";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSymbolAction(Analyze, SymbolKind.NamedType);
        }

        private void Analyze(SymbolAnalysisContext context)
        {
            var type = (INamedTypeSymbol)context.Symbol;

            if (type.TypeKind != TypeKind.Class) return;

            if (!IsAttribute(type)) return;

            if (type.IsAbstract || type.IsSealed) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, type.Locations[0], type.Name));
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