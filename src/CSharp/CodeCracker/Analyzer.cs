using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCracker
{
    public abstract class Analyzer : DiagnosticAnalyzer
    {
        public List<DiagnosticDescriptor> DiagnosticDescriptors { get; } = new List<DiagnosticDescriptor>();
        protected Analyzer(DiagnosticDescriptorInfo diagnosticDescriptorInfo)
        {
            DiagnosticDescriptors.Add(diagnosticDescriptorInfo.ToDiagnosticDescriptor());
        }
        protected Analyzer(DiagnosticDescriptorInfo diagnosticDescriptorInfo1, DiagnosticDescriptorInfo diagnosticDescriptorInfo2)
        {
            DiagnosticDescriptors.Add(diagnosticDescriptorInfo1.ToDiagnosticDescriptor());
            DiagnosticDescriptors.Add(diagnosticDescriptorInfo2.ToDiagnosticDescriptor());
        }
        protected Analyzer(DiagnosticDescriptorInfo diagnosticDescriptorInfo1, DiagnosticDescriptorInfo diagnosticDescriptorInfo2,
            params DiagnosticDescriptorInfo[] diagnosticDescriptorInfos) : this(diagnosticDescriptorInfo1, diagnosticDescriptorInfo2)
        {
            foreach (var d in diagnosticDescriptorInfos) DiagnosticDescriptors.Add(d.ToDiagnosticDescriptor());
        }
        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => DiagnosticDescriptors.ToImmutableArray();
        public abstract override void Initialize(AnalysisContext context);
        public void ReportDiagnostic(SymbolAnalysisContext context, Location location, params object[] messageArgs) =>
            ReportDiagnostic(context, location, 0, messageArgs);
        public void ReportDiagnostic(SymbolAnalysisContext context, Location location, int diagnosticIndex = 0, params object[] messageArgs) =>
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors[diagnosticIndex], location, messageArgs));
        public void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location, params object[] messageArgs) =>
            ReportDiagnostic(context, location, 0, messageArgs);
        public void ReportDiagnostic(SyntaxNodeAnalysisContext context, Location location, int diagnosticIndex = 0, params object[] messageArgs) =>
            context.ReportDiagnostic(Diagnostic.Create(DiagnosticDescriptors[diagnosticIndex], location, messageArgs));
    }
}
