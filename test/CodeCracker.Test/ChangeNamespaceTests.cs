using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ChangeNamespaceTests : CodeFixTest<ChangeNamespaceAnalyzer, ChangeNamespaceCodeFixProvider>
    {
        string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            
        }
    }";

        [Fact]
        public void ChangeNamespaceAnalyzerCreateDiagnostic()
        {
            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void WhenChangeNamespaceStatement()
        {

            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
           
        }
    }";
            VerifyCSharpFix(test, fixtest, 0, false, true);
        }        
    }
}