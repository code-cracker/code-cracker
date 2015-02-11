using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Test.Verifiers
{
    public abstract class DiagnosticTest<T> : DiagnosticVerifier where T : DiagnosticAnalyzer, new()
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new T();
        }
    }
}