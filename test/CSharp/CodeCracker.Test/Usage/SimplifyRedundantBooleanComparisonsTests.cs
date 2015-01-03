using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Usage
{
    public class SimplifyRedundantBooleanComparisonsTests
        : CodeFixTest<SimplifyRedundantBooleanComparisonsAnalyzer, SimplifyRedundantBooleanComparisonsCodeFixProvider>
    {
        [Theory]
        [InlineData("if (foo == true) {}", 21)]
        [InlineData("var fee = (foo == true);", 28)]
        [InlineData("if (foo == false) {}", 21)]
        [InlineData("var fee = (foo == false);", 28)]
        [InlineData("if (foo != true) {}", 21)]
        [InlineData("var fee = (foo != true);", 28)]
        [InlineData("if (foo != false) {}", 21)]
        [InlineData("var fee = (foo != false);", 28)]
        public async Task WhenComparingWithBoolAnalyzerCreatesDiagnostic(string sample, int column)
        {
            var test = sample.WrapInMethod();

            var expected = new DiagnosticResult
            {
                Id = SimplifyRedundantBooleanComparisonsAnalyzer.DiagnosticId,
                Message = "You can remove this comparison.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, column) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }


        [Theory]
        [InlineData("if (foo == 0) {}")]
        [InlineData("var fee = (foo == 0);")]
        [InlineData("if (foo != 0) {}")]
        [InlineData("var fee = (foo != 0);")]
        public async Task IgnoresWhenComparingWithNotBool(string sample)
        {
            var test = sample.WrapInMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Theory]
        [InlineData("if (foo == true) {}", "if (foo) {}" )]
        [InlineData("var fee = (foo == true);", "var fee = (foo);")]
        [InlineData("if (foo == false) {}", "if (!foo) {}")]
        [InlineData("var fee = (foo == false);", "var fee = (!foo);")]
        [InlineData("if (foo != true) {}", "if (!foo) {}")]
        [InlineData("var fee = (foo != true);", "var fee = (!foo);")]
        [InlineData("if (foo != false) {}", "if (foo) {}")]
        [InlineData("var fee = (foo != false);", "var fee = (foo);")]
        public async Task FixRemovesRedundantComparisons(string original, string result)
        {
            var test = original.WrapInMethod();
            var fixtest = result.WrapInMethod();

            await VerifyCSharpFixAsync(test, fixtest);
        }
    }
}