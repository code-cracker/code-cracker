using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class UnnecessaryParenthesisTests : CodeFixVerifier<UnnecessaryParenthesisAnalyzer, UnnecessaryParenthesisCodeFixProvider>
    {
        [Fact]
        public async Task ConstructorWithEmptyParenthesisWithInitializerTriggersFix()
        {
            const string source = @"var a = new B() { X = 1 };";
            var expected = new DiagnosticResult(DiagnosticId.UnnecessaryParenthesis.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(1, 14)
                .WithMessage("Remove unnecessary parenthesis.");

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