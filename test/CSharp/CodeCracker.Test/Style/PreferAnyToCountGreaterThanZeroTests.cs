using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class PreferAnyToCountGreaterThanZeroTests : CodeFixVerifier<PreferAnyToCountGreaterThanZeroAnalyzer, PreferAnyToCountGreaterThanZeroCodeFixProvider>
    {
        private const string test = @"
    using System;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                var ints = new[] { 1, 2 };
                var query = true && ints.Count() > 0 && true;
            }
        }
    }";

        [Fact]
        public async Task CreatesDiagnosticsWhenUsingCountMethodGreaterThanZero()
        {
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.PreferAnyToCountGreaterThanZero.ToDiagnosticId(),
                Message = string.Format(PreferAnyToCountGreaterThanZeroAnalyzer.MessageFormat.ToString(), ""),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 37) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreatesDiagnosticsWhenUsingCountPropertyGreaterThanZero()
        {
            const string test = @"
class Bar
{
    void Foo()
    {
        var ints = new System.Collections.Generic.List<int>();
        var query = true && ints.Count > 0 && true;
    }
}";
            var a = new System.Collections.Generic.List<int>();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.PreferAnyToCountGreaterThanZero.ToDiagnosticId(),
                Message = string.Format(PreferAnyToCountGreaterThanZeroAnalyzer.MessageFormat.ToString(), ""),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 29) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }


        [Fact]
        public async Task IgnoreNestedInvocationOfCount()
        {
            const string testCountProp = @"
    using System;
    using System.Linq;
    namespace ConsoleApplication1
    {
        class Program
        {
            public override void M()
            {
                var b = Foo(new[] { 2 }.Count()) > 0;
            }
            static int Foo(object i) => 1;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(testCountProp);
        }

        [Fact]
        public async Task IgnoresCountWithoutZero()
        {
            const string testCountProp = @"
    using System;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                var list = new List<int>();
                var query = list.Count() > 1;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(testCountProp);
        }

        [Fact]
        public async Task ConvertsCountPropertyToAny()
        {
            const string testCountProp = @"
    using System;
    using System.Collections.Generic;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                var list = new List<int>();
                var query = list.Count > 0;
            }
        }
    }";

            const string fixTest = @"
    using System;
    using System.Collections.Generic;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                var list = new List<int>();
                var query = list.Any();
            }
        }
    }";
            await VerifyCSharpFixAsync(testCountProp, fixTest);
        }

        [Fact]
        public async Task ConvertsCountMethodToAny()
        {

            const string fixTest = @"
    using System;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                var ints = new[] { 1, 2 };
                var query = true && ints.Any() && true;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, fixTest);
        }


        [Fact]
        public async Task ConvertsToAnyWithPredicate()
        {
            const string testPredicate = @"
    using System;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class Program
        {
            class Bar
            {
                public bool A { get; set; }
            }

            public async Task Foo()
            {
                var bools = new[] { new Bar(), new Bar() };
                var queryB = bools.Count(x => x.A) > 0;
            }
        }
    }";

            const string fixTest = @"
    using System;
    using System.Linq;

    namespace ConsoleApplication1
    {
        class Program
        {
            class Bar
            {
                public bool A { get; set; }
            }

            public async Task Foo()
            {
                var bools = new[] { new Bar(), new Bar() };
                var queryB = bools.Any(x => x.A);
            }
        }
    }";
            await VerifyCSharpFixAsync(testPredicate, fixTest);
        }
    }
}