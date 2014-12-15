using CodeCracker.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Performance
{
    public class RemoveWhereWhenItIsPossibleTests : CodeFixTest<RemoveWhereWhenItIsPossibleAnalyzer, RemoveWhereWhenItIsPossibleCodeFixProvider>
    {
        [Theory]
        [InlineData("First")]
        [InlineData("FirstOrDefault")]
        [InlineData("Last")]
        [InlineData("LastOrDefault")]
        [InlineData("Any")]
        [InlineData("Single")]
        [InlineData("SingleOrDefault")]
        [InlineData("Count")]
        public async Task CreateDiagnosticWhenUsingWhereWith(string method)
        {
            var test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public async Task DoSomething()
        {
            var a = new int[10];
            var f = a.Where(item => item > 10)." + method + @"();
        }
    }
}";

            var expected = new DiagnosticResult
            {
                Id = RemoveWhereWhenItIsPossibleAnalyzer.DiagnosticId,
                Message = "You can remove 'Where' moving the predicate to '" + method + "'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 23) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);

        }

        [Theory]
        [InlineData("First")]
        [InlineData("FirstOrDefault")]
        [InlineData("Last")]
        [InlineData("LastOrDefault")]
        [InlineData("Any")]
        [InlineData("Single")]
        [InlineData("SingleOrDefault")]
        [InlineData("Count")]
        public async Task DoNotCreateDiagnosticWhenUsingWhereAndAnotherMethodWithPredicates(string method)
        {
            var test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public async Task DoSomething()
        {
            var a = new int[10];
            var f = a.Where(item => item > 10)." + method + @"(item => item < 50);
        }
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }


        [Theory]
        [InlineData("First")]
        [InlineData("FirstOrDefault")]
        [InlineData("Last")]
        [InlineData("LastOrDefault")]
        [InlineData("Any")]
        [InlineData("Single")]
        [InlineData("SingleOrDefault")]
        [InlineData("Count")]
        public async Task FixRemovesWhereMovingPredicateTo(string method)
        {
            var test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public async Task DoSomething()
        {
            var a = new int[10];
            var f = a.Where(item => item > 10)." + method + @"();
        }
    }
}";

            var expected = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public async Task DoSomething()
        {
            var a = new int[10];
            var f = a." + method + @"(item => item > 10);
        }
    }
}";

            await VerifyCSharpFixAsync(test, expected);

        }

        [Theory]
        [InlineData("First")]
        [InlineData("FirstOrDefault")]
        [InlineData("Last")]
        [InlineData("LastOrDefault")]
        [InlineData("Any")]
        [InlineData("Single")]
        [InlineData("SingleOrDefault")]
        [InlineData("Count")]
        public async Task FixRemovesWherePreservingPreviousExpressionsMovingPredicateTo(string method)
        {
            var test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public async Task DoSomething()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(item => item > 10)." + method + @"();
        }
    }
}";

            var expected = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public async Task DoSomething()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item)." + method + @"(item => item > 10);
        }
    }
}";

            await VerifyCSharpFixAsync(test, expected);

        }
    }
}