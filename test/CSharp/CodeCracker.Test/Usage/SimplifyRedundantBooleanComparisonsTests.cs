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
        [InlineData("if (true == foo) {}", 21)]
        [InlineData("var fee = (foo == true);", 28)]
        [InlineData("var fee = (true == foo);", 28)]
        [InlineData("if (foo == false) {}", 21)]
        [InlineData("if (false == foo) {}", 21)]
        [InlineData("var fee = (foo == false);", 28)]
        [InlineData("var fee = (false == foo);", 28)]
        [InlineData("if (foo != true) {}", 21)]
        [InlineData("if (true != foo) {}", 21)]
        [InlineData("var fee = (foo != true);", 28)]
        [InlineData("var fee = (true != foo);", 28)]
        [InlineData("if (foo != false) {}", 21)]
        [InlineData("if (false != foo) {}", 21)]
        [InlineData("var fee = (foo != false);", 28)]
        [InlineData("var fee = (false != true);", 28)]
        public async Task WhenComparingWithBoolAnalyzerCreatesDiagnostic(string sample, int column)
        {
            sample = "bool foo; " + sample; // add declaration of foo
            column += 10;                   // adjust column for added declaration
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
        [InlineData("if (0 == foo) {}")]
        [InlineData("var fee = (foo == 0);")]
        [InlineData("var fee = (0 == foo);")]
        [InlineData("if (foo != 0) {}")]
        [InlineData("if (0 != foo) {}")]
        [InlineData("if (foo == default(bool?)) {}")]
        [InlineData("if (default(bool?) == foo) {}")]
        [InlineData("var fee = (foo != 0);")]
        [InlineData("var fee = (0 != foo);")]
        [InlineData("var fee = (foo == bar);")]
        [InlineData("var fee = (bar == foo);")]
        [InlineData("var fee = (foo != bar);")]
        [InlineData("var fee = (bar != foo);")]
        public async Task IgnoresWhenComparingWithNotBool(string sample)
        {
            sample = "bool foo; " + sample;
            var test = sample.WrapInMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Theory]
        [InlineData("if ( == ) {}")]
        [InlineData("if (foo == ) {}")]
        [InlineData("if ( == foo) {}")]
        public async Task IgnoresIncompleteComparisonExpression(string sample)
        {
            sample = "bool foo; " + sample;
            var test = sample.WrapInMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Theory]
        [InlineData("if (foo == true) {}", "if (foo) {}" )]
        [InlineData("if (true == foo) {}", "if (foo) {}")]
        [InlineData("var fee = (foo == true);", "var fee = (foo);")]
        [InlineData("var fee = (true == foo);", "var fee = (foo);")]
        [InlineData("if (foo == false) {}", "if (!foo) {}")]
        [InlineData("if (false == foo) {}", "if (!foo) {}")]
        [InlineData("var fee = (foo == false);", "var fee = (!foo);")]
        [InlineData("var fee = (false == foo);", "var fee = (!foo);")]
        [InlineData("if (foo != true) {}", "if (!foo) {}")]
        [InlineData("if (true != foo) {}", "if (!foo) {}")]
        [InlineData("var fee = (foo != true);", "var fee = (!foo);")]
        [InlineData("var fee = (true != foo);", "var fee = (!foo);")]
        [InlineData("if (foo != false) {}", "if (foo) {}")]
        [InlineData("if (false != foo) {}", "if (foo) {}")]
        [InlineData("var fee = (foo != false);", "var fee = (foo);")]
        [InlineData("var fee = (false != foo);", "var fee = (foo);")]
        public async Task FixRemovesRedundantComparisons(string original, string result)
        {
            original = "bool foo;" + original;
            result = "bool foo; " + result;
            var test = original.WrapInMethod();
            var fixtest = result.WrapInMethod();

            await VerifyCSharpFixAsync(test, fixtest);
        }
    }
}