using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class EmptyObjectInitializerTests : CodeFixTest<EmptyObjectInitializerAnalyzer, EmptyObjectInitializerCodeFixProvider>
    {
        [Fact]
        public void EmptyObjectInitializerTriggersFix()
        {
            var code = @"var a = new A {};";
            var expected = new DiagnosticResult
            {
                Id = EmptyObjectInitializerAnalyzer.DiagnosticId,
                Message = "Remove empty object initializer.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 15) }
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
    }
}