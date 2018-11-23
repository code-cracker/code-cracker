using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class RemoveWhereWhenItIsPossibleTests : CodeFixVerifier<RemoveWhereWhenItIsPossibleAnalyzer, RemoveWhereWhenItIsPossibleCodeFixProvider>
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
                Id = DiagnosticId.RemoveWhereWhenItIsPossible.ToDiagnosticId(),
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

        [Fact]
        public async Task DoNotCreateDiagnosticWhenWhereUsesIndexer()
        {
            var test = @"
var first = Enumerable.Range(1, 10).ToList();
var second = Enumerable.Range(1, 10);
var isNotMatch = second.Where((t, i) => first[i] != t).Any();
".WrapInCSharpMethod(usings: "using System.Linq;");
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
            var f = a.Where((item) => item > 10)." + method + @"();
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
            var f = a." + method + @"((item) => item > 10);
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

        // Async
        [Theory]
        [InlineData("FirstAsync")]
        [InlineData("FirstOrDefaultAsync")]
        [InlineData("LastAsync")]
        [InlineData("LastOrDefaultAsync")]
        [InlineData("AnyAsync")]
        [InlineData("SingleAsync")]
        [InlineData("SingleOrDefaultAsync")]
        [InlineData("CountAsync")]
        public async Task CreateDiagnosticWhenUsingWhereWithAsync(string method)
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
                Id = DiagnosticId.RemoveWhereWhenItIsPossible.ToDiagnosticId(),
                Message = "You can remove 'Where' moving the predicate to '" + method + "'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 23) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);

        }

        [Theory]
        [InlineData("FirstAsync")]
        [InlineData("FirstOrDefaultAsync")]
        [InlineData("LastAsync")]
        [InlineData("LastOrDefaultAsync")]
        [InlineData("AnyAsync")]
        [InlineData("SingleAsync")]
        [InlineData("SingleOrDefaultAsync")]
        [InlineData("CountAsync")]
        public async Task DoNotCreateDiagnosticWhenUsingWhereAndAnotherMethodWithPredicatesAsync(string method)
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

        [Fact]
        public async Task DoNotCreateDiagnosticWhenWhereUsesIndexerAsync()
        {
            var test = @"
var first = Enumerable.Range(1, 10).ToList();
var second = Enumerable.Range(1, 10);
var isNotMatch = second.Where((t, i) => first[i] != t).Any();
".WrapInCSharpMethod(usings: "using System.Linq;");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Theory]
        [InlineData("FirstAsync")]
        [InlineData("FirstOrDefaultAsync")]
        [InlineData("LastAsync")]
        [InlineData("LastOrDefaultAsync")]
        [InlineData("AnyAsync")]
        [InlineData("SingleAsync")]
        [InlineData("SingleOrDefaultAsync")]
        [InlineData("CountAsync")]
        public async Task FixRemovesWhereMovingPredicateToAsync(string method)
        {
            var test = @"
namespace Sample
{
    public class Foo
    {
        public async Task DoSomething()
        {
            var a = new int[10];
            var f = a.Where((item) => item > 10)." + method + @"();
        }
    }
}";
            var expected = @"
namespace Sample
{
    public class Foo
    {
        public async Task DoSomething()
        {
            var a = new int[10];
            var f = a." + method + @"((item) => item > 10);
        }
    }
}";
            await VerifyCSharpFixAsync(test, expected);
        }

        [Theory]
        [InlineData("FirstAsync")]
        [InlineData("FirstOrDefaultAsync")]
        [InlineData("LastAsync")]
        [InlineData("LastOrDefaultAsync")]
        [InlineData("AnyAsync")]
        [InlineData("SingleAsync")]
        [InlineData("SingleOrDefaultAsync")]
        [InlineData("CountAsync")]
        public async Task FixRemovesWherePreservingPreviousExpressionsMovingPredicateToAsync(string method)
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