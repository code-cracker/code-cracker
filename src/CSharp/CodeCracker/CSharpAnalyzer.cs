using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public abstract class CSharpAnalyzer : Analyzer
    {
        protected CSharpAnalyzer(DiagnosticDescriptorInfo diagnosticDescriptorInfo) : base(diagnosticDescriptorInfo) { }
        protected CSharpAnalyzer(DiagnosticDescriptorInfo diagnosticDescriptorInfo1,
            DiagnosticDescriptorInfo diagnosticDescriptorInfo2) : base(diagnosticDescriptorInfo1, diagnosticDescriptorInfo2) { }
        protected CSharpAnalyzer(DiagnosticDescriptorInfo diagnosticDescriptorInfo1,
            DiagnosticDescriptorInfo diagnosticDescriptorInfo2,
            params DiagnosticDescriptorInfo[] diagnosticDescriptorInfos)
            : base(diagnosticDescriptorInfo1, diagnosticDescriptorInfo2, diagnosticDescriptorInfos) { }
    }
}
