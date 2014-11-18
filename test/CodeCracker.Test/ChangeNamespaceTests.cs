using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ChangeNamespaceTests : CodeFixTest<ChangeNamespaceAnalyzer, ChangeNamespaceCodeFixProvider>
    {
        [Fact]
        public void ChangeNamespaceAnalyzerCreateDiagnostic()
        {
            string test = @"
    using System;

    namespace Temp
    {
        class TypeName
        {
            
        }
    }";
            DiagnosticVerifier.TempPath = @"c:\Temp";
            VerifyCSharpHasNoDiagnostics(test);
            DiagnosticVerifier.TempPath = "";
        }

        [Fact]
        public void WhenChangeNamespaceStatement()
        {
            string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            
        }
    }";

            var fixtest = @"
    using System;

    namespace Temp
    {
        class TypeName
        {
           
        }
    }";
            DiagnosticVerifier.TempPath = @"c:\Temp";
            VerifyCSharpFix(test, fixtest, 0, false, true);
            DiagnosticVerifier.TempPath = "";
        }
    }
}