using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class EmptyObjectInitializerTests : CodeFixVerifier<EmptyObjectInitializerAnalyzer, EmptyObjectInitializerCodeFixProvider>
    {
        [Fact]
        public async Task EmptyObjectInitializerTriggersFix()
        {
            const string code = @"var a = new A {};";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.EmptyObjectInitializer.ToDiagnosticId(),
                Message = "Remove the empty object initializer.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 15) }
            };

            await VerifyCSharpDiagnosticAsync(code, expected);
        }

        [Fact]
        public async Task EmptyObjectInitializerIsRemoved()
        {
            const string oldCode = @"var a = new A() {};";
            const string newCode = @"var a = new A();";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task EmptyObjectInitializerWithNoArgsIsRemovedAndAddsEmptyArgs()
        {
            const string oldCode = @"var a = new A {};";
            const string newCode = @"var a = new A();";

            await VerifyCSharpFixAsync(oldCode, newCode);
        }

        [Fact]
        public async Task FilledObjectInitializerIsIgnored()
        {
            const string code = @"var a = new A { X = 1 };";
            await VerifyCSharpHasNoDiagnosticsAsync(code);
        }

        [Fact]
        public async Task AbsenceOfObjectInitializerIsIgnored()
        {
            const string code = @"var a = new A();";
            await VerifyCSharpHasNoDiagnosticsAsync(code);
        }
    }
}
