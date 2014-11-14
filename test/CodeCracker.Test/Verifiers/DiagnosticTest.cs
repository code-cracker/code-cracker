using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelper;

namespace CodeCracker.Test.Verifiers
{
    public abstract class DiagnosticTest<T> : DiagnosticVerifier
        where T : DiagnosticAnalyzer, new()
    {
        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new T();
        }
    }
}
