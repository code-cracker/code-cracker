using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.CSharp.Test.Style
{
    public class UnnecessaryParenthesisTests : CodeFixTest<UnnecessaryParenthesisAnalyzer, UnnecessaryParenthesisCodeFixProvider>
    {
        [Fact]
        public async Task ConstructorWithEmptyParenthesisWithInitializerTriggersFix()
        {
            const string source = @"var a = new B() { X = 1 };";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UnnecessaryParenthesis.ToDiagnosticId(),
                Message = "Remove unnecessary parenthesis.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 14) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ConstructorWithoutParenthesisWithInitializerIsIgnored()
        {
            const string source = @"new B { X = 1 };";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConstructorWithEmptyParenthesisWithoutInitializerIsIgnored()
        {
            const string source = @"new B();";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConstructorWithArgumentsWithInitializerIsIgnored()
        {
            const string source = @"new Sample(1) { A = 2 };";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConstructorWithArgumentsWithoutInitializerIsIgnored()
        {
            const string source = @"new Sample(1);";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConstructorWithoutArgumentsWithEmptyInitializerIsIgnored()
        {
            const string source = @"new Sample { };";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task RemoveUnnecessaryParenthesisInConstructorCall()
        {
            const string oldSource = @"var a = new B() { X = 1 };";
            const string newSource = @"var a = new B { X = 1 };";

            await VerifyCSharpFixAsync(oldSource, newSource);
        }
    }
}