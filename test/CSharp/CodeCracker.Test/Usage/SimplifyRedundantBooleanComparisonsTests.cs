using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class SimplifyRedundantBooleanComparisonsTests
        : CodeFixVerifier<SimplifyRedundantBooleanComparisonsAnalyzer, SimplifyRedundantBooleanComparisonsCodeFixProvider>
    {
        [Theory]
        [InlineData("if (foo == true) {}", 17)]
        [InlineData("if (true == foo) {}", 17)]
        [InlineData("var fee = (foo == true);", 24)]
        [InlineData("var fee = (true == foo);", 24)]
        [InlineData("if (foo == false) {}", 17)]
        [InlineData("if (false == foo) {}", 17)]
        [InlineData("var fee = (foo == false);", 24)]
        [InlineData("var fee = (false == foo);", 24)]
        [InlineData("if (foo != true) {}", 17)]
        [InlineData("if (true != foo) {}", 17)]
        [InlineData("var fee = (foo != true);", 24)]
        [InlineData("var fee = (true != foo);", 24)]
        [InlineData("if (foo != false) {}", 17)]
        [InlineData("if (false != foo) {}", 17)]
        [InlineData("var fee = (foo != false);", 24)]
        [InlineData("var fee = (false != true);", 24)]
        public async Task WhenComparingWithBoolAnalyzerCreatesDiagnostic(string sample, int column)
        {
            sample = "bool foo; " + sample; // add declaration of foo
            column += 10;                   // adjust column for added declaration
            var test = sample.WrapInCSharpMethod();

            var expected = new DiagnosticResult(DiagnosticId.SimplifyRedundantBooleanComparisons.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, column)
                .WithMessage("You can remove this comparison.");

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
            var test = sample.WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Theory]
        [InlineData("if ( == ) {}")]
        [InlineData("if (foo == ) {}")]
        [InlineData("if ( == foo) {}")]
        public async Task IgnoresIncompleteComparisonExpression(string sample)
        {
            sample = "bool foo; " + sample;
            var test = sample.WrapInCSharpMethod();
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
            var test = original.WrapInCSharpMethod();
            var fixtest = result.WrapInCSharpMethod();

            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task FixWithoutThrowingAnyException()
        {
            const string test = @"
struct ProjectCompilation {}
class Foo
{
    public bool Comp(bool obj)
    {
        if (obj is ProjectCompilation == false) return false;
        return true;
    }
}";

            const string fixtest = @"
struct ProjectCompilation {}
class Foo
{
    public bool Comp(bool obj)
    {
        if (!(obj is ProjectCompilation)) return false;
        return true;
    }
}";
            await VerifyCSharpFixAsync(test, fixtest);
        }
    }
}