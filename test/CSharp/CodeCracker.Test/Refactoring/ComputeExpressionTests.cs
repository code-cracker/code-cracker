using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class ComputeExpressionTests : CodeFixVerifier<ComputeExpressionAnalyzer, ComputeExpressionCodeFixProvider>
    {
        [Fact]
        public async Task BinaryExpressionWithoutLiteralOnRightDoesNotCreateDiagnostic()
        {
            var source = "var i = 1;var a = 1 + i;".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task BinaryExpressionWithoutLiteralOnLeftDoesNotCreateDiagnostic()
        {
            var source = "var i = 1;var a = i + 1;".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [InlineData("var a = 1 + 1;", 8, 0)]
        [InlineData("var a = 1 - 1;", 8, 0)]
        [InlineData("var a = 4 * 3;", 8, 0)]
        [InlineData("var a = 12 / 3;", 8, 0)]
        [InlineData("var a = 12.0 / 5;", 8, 0)]
        [InlineData("var a = 12 / 5;", 8, 0)]
        [InlineData("var a = 1 + 1 - 2;", 8, 0)]
        [InlineData("var a = (1 + 1);", 8, 0)]
        [InlineData("var a = 1 + (1 - 2);", 8, 0)]
        [InlineData("var a = 3 * (2 + 7.0 / (2 - 1)) * (1 - 2);", 8, 0)]
        [InlineData("var a = 1m * (1 + 2) * 3;", 8, 0)]
        [InlineData("System.Console.WriteLine(1m * (1 + 2) * 3);", 25, 1)]
        public async Task BinaryExpressionWithLiteralOnLeftAndRightCreatesDiagnostic(string original, int columnOffset, int columnRightTrim)
        {
            var source = original.WrapInCSharpMethod();
            var expression = original.Substring(columnOffset, original.Length - columnOffset - columnRightTrim - 1);
            var expected = new DiagnosticResult(DiagnosticId.ComputeExpression.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 13 + columnOffset)
                .WithMessage(string.Format(ComputeExpressionAnalyzer.Message, expression));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Theory]
        [InlineData("var a = 1 + 1;", "var a = 2;")]
        [InlineData("var a = 1 - 1;", "var a = 0;")]
        [InlineData("var a = 4 * 3;", "var a = 12;")]
        [InlineData("var a = 12 / 3;", "var a = 4;")]
        [InlineData("var a = 12.0 / 5;", "var a = 2.4;")]
        [InlineData("var a = 12 / 5;", "var a = 2;")]
        [InlineData("var a = 1 + 1 - 2;", "var a = 0;")]
        [InlineData("var a = (1 + 1);", "var a = 2;")]
        [InlineData("var a = 3 * (2 + 7.0 / (2 - 1)) * (1 - 2);", "var a = -27;")]
        [InlineData("var a = 1m * (1 + 2) * 3.1m;", "var a = 9.3;")]
        public async Task ComputeExpression(string original, string fix) =>
            await VerifyCSharpFixAsync(original.WrapInCSharpMethod(), fix.WrapInCSharpMethod());

        [Fact]
        public async Task ComputeExpressionOnADifferentCulture()
        {
            using (new ChangeCulture("pt-BR"))
                await VerifyCSharpFixAsync("var a = 1m * (1 + 2) * 3.1m;".WrapInCSharpMethod(), "var a = 9.3;".WrapInCSharpMethod());
        }

        [Fact]
        public async Task IncorrectExpressionDoesNotCreateDiagnostic() =>
            await VerifyCSharpHasNoDiagnosticsAsync("var a = 1m * (1 + 2) * 3.1;".WrapInCSharpMethod());

        [Fact]
        public async Task CompilerErrorDoesNotRegisterAFix() =>
            await VerifyCSharpHasNoFixAsync("var a = 1m * 3.1;".WrapInCSharpMethod());

        [Fact]
        public async Task ExpressionThatThrowsDoesNotRegisterAFix() =>
            await VerifyCSharpHasNoFixAsync("var a = int.MaxValue + int.MaxValue;".WrapInCSharpMethod());

        [Fact]
        public async Task ExpressionOnArgumentsFix()
        {
            var source = "string.Format(\"2 Hours in minutes: {0}\", 60 * 2)".WrapInCSharpMethod();
            var fix = "string.Format(\"2 Hours in minutes: {0}\", 120)".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fix);
        }
    }
}