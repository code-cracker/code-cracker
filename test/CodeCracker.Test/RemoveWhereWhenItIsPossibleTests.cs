using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;
using Xunit.Extensions;

namespace CodeCracker.Test
{
    public class RemoveWhereWhenItIsPossibleTests
         : CodeFixTest<RemoveWhereWhenItIsPossibleAnalyzer, RemoveWhereWhenItIsPossibleCodeFixProvider>
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
        public void CreateDiagnosticWhenUsingWhereWith(string method)
        {
            string test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public void DoSomething()
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

            VerifyCSharpDiagnostic(test, expected);

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
        public void DoNotCreateDiagnosticWhenUsingWhereAndAnotherMethodWithPredicates(string method)
        {
            string test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public void DoSomething()
        {
            var a = new int[10];
            var f = a.Where(item => item > 10)." + method + @"(item => item < 50);
        }
    }
}";

            VerifyCSharpHasNoDiagnostics(test);

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
        public void FixRemovesWhereMovingPredicateTo(string method)
        {
            string test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public void DoSomething()
        {
            var a = new int[10];
            var f = a.Where(item => item > 10)." + method + @"();
        }
    }
}";

            string expected = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public void DoSomething()
        {
            var a = new int[10];
            var f = a." + method + @"(item => item > 10);
        }
    }
}";

            VerifyCSharpFix(test, expected);

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
        public void FixRemovesWherePreservingPreviousExpressionsMovingPredicateTo(string method)
        {
            string test = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public void DoSomething()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item).Where(item => item > 10)." + method + @"();
        }
    }
}";

            string expected = @"
using System.Linq;

namespace Sample
{
    public class Foo
    {
        public void DoSomething()
        {
            var a = new int[10];
            var f = a.OrderBy(item => item)." + method + @"(item => item > 10);
        }
    }
}";

            VerifyCSharpFix(test, expected);

        }
    }
}
