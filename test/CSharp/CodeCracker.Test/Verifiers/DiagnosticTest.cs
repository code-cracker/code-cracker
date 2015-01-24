using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;

namespace CodeCracker.Test.Verifiers
{
    public abstract class DiagnosticTest<T> : DiagnosticVerifier where T : DiagnosticAnalyzer, new()
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new T();
        }
    }
}