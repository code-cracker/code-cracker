using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class NumericLiteralTests : CodeFixVerifier<NumericLiteralAnalyzer, NumericLiteralCodeFixProvider>
    {
        [Theory]
        [InlineData("\"")]
        [InlineData("1.1")]
        [InlineData("1F")]
        [InlineData("1f")]
        [InlineData("1D")]
        [InlineData("1d")]
        [InlineData("1M")]
        [InlineData("1m")]
        [InlineData("1e2")]
        public async Task IfNotAIntegerNumericLiteralDoesNotCreateDiagnostic(string literal)
        {
            var source = $"var a = {literal};".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [InlineData("1", true)]
        [InlineData("0x1", false)]
        [InlineData("0x1F", false)]
        public async Task IntegerLiteralCreatesDiagnostic(string literal, bool isDecimal)
        {
            var source = $"var a = {literal};".WrapInCSharpMethod();
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnosticResult(literal, isDecimal));
        }

        [Fact]
        public async Task FixLiteralAsArgument()
        {
            var source = @"string.Format("""", 1);".WrapInCSharpMethod();
            var fixtest = @"string.Format("""", 0x1);".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Theory]
        [InlineData("1", "0x1")]
        [InlineData("0x1", "1")]
        [InlineData("12345678", "0xBC614E")]
        [InlineData("-12345678", "0xFF439EB2")]
        [InlineData("0xBC614E", "12345678")]
        public async Task FixReplacesLiteral(string literal, string fixedLiteral)
        {
            var source = $"var a = {literal};".WrapInCSharpMethod();
            var fixtest = $"var a = {fixedLiteral};".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixOnMethodCallReplacesLiteral()
        {
            var source = @"
    class TypeName
    {
        void Foo()
        {
            Bar(12345678);
        }
        int Bar(int i) => i;
    }
".WrapInCSharpMethod();
            var fixtest = @"
    class TypeName
    {
        void Foo()
        {
            Bar(0xBC614E);
        }
        int Bar(int i) => i;
    }
".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        private static DiagnosticResult CreateDiagnosticResult(string literal, bool isDecimal, int row = 10, int col = 21)
        {
            return new DiagnosticResult(DiagnosticId.NumericLiteral.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(row, col)
                .WithMessage(string.Format(NumericLiteralAnalyzer.Message, literal, isDecimal ? "hexadecimal" : "decimal"));
        }
    }
}