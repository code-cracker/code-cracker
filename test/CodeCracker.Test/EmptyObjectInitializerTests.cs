using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class EmptyObjectInitializerTests : CodeFixTest<EmptyObjectInitializerAnalyzer, EmptyObjectInitializerCodeFixProvider>
    {
        [Fact]
        public async Task EmptyObjectInitializerTriggersFix()
        {
            var code = @"var a = new A {};";
            var expected = new DiagnosticResult
            {
                Id = EmptyObjectInitializerAnalyzer.DiagnosticId,
                Message = "Remove empty object initializer.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 15) }
            };

            await VerifyCSharpDiagnosticAsync(code, expected);
        }

        [Fact]
        public async Task EmptyObjectInitializerIsRemoved()
        {
            var oldCode = @"var a = new A() {};";
            var newCode = @"var a = new A();";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task EmptyObjectInitializerWithNoArgsIsRemovedAndAddsEmptyArgs()
        {
            var oldCode = @"var a = new A {};";
            var newCode = @"var a = new A();";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task FilledObjectInitializerIsIgnored()
        {
            var code = @"var a = new A { X = 1 };";
            await VerifyCSharpHasNoDiagnosticsAsync(code);
        }

        [Fact]
        public async Task AbsenceOfObjectInitializerIsIgnored()
        {
            var code = @"var a = new A();";
            await VerifyCSharpHasNoDiagnosticsAsync(code);
        }
    }
}