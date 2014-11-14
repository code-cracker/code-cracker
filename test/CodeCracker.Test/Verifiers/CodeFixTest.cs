using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace TestHelper
{
    public abstract class CodeFixTest<T, U> : CodeFixVerifier
        where T : DiagnosticAnalyzer, new()
        where U : CodeFixProvider, new()
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new T();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new U();
        }        
    }
}
