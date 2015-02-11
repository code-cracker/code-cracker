using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Test
{
    public abstract class CodeFixTest<T, U> : CodeFixVerifier
        where T : DiagnosticAnalyzer, new()
        where U : CodeFixProvider, new()
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer() => new T();

        protected override CodeFixProvider GetCSharpCodeFixProvider() => new U();
    }
}