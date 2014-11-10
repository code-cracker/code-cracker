using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TestHelper;
using Xunit;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeCracker.Test
{
    public class EmptyObjectInitializerTests : CodeFixVerifier
    {
        [Fact]
        public void EmptyObjectInitializerTriggersFix()
        {
            var code = @"var a = new A {};";
            var expected = new DiagnosticResult
            {
                Id = EmptyObjectInitializerAnalyzer.DiagnosticId,
                Message = "Remove empty object initializer.",
                Severity = DiagnosticSeverity.Error,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 1, 15)
                        }
            };

            VerifyCSharpDiagnostic(code, expected);
        }

        [Fact]
        public void EmptyObjectInitializerIsRemoved()
        {
            var oldCode = @"var a = new A() {};";
            var newCode = @"var a = new A();";

            VerifyCSharpFix(oldCode, newCode);
        }

        [Fact]
        public void EmptyObjectInitializerWithNoArgsIsRemovedAndAddsEmptyArgs()
        {
            var oldCode = @"var a = new A {};";
            var newCode = @"var a = new A();";

            VerifyCSharpFix(oldCode, newCode);
        }

        [Fact]
        public void FilledObjectInitializerIsIgnored()
        {
            var code = @"var a = new A { X = 1 };";
            VerifyCSharpHasNoDiagnostics(code);
        }

        [Fact]
        public void AbsenceOfObjectInitializerIsIgnored()
        {
            var code = @"var a = new A();";
            VerifyCSharpHasNoDiagnostics(code);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new EmptyObjectInitializerAnalyzer();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new EmptyObjectInitializerCodeFixProvider();
        }
    }
}
