using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
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
            var expected = new DiagnosticResult(DiagnosticId.RemoveWhereWhenItIsPossible.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(11, 23)
                .WithMessage("You can remove 'Where' moving the predicate to '" + method + "'.");

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
    }
}