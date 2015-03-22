using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.Test.CSharp.Performance
{
    public class UseStaticRegexIsMatchTests : CodeFixVerifier<UseStaticRegexIsMatchAnalyzer, UseStaticRegexIsMatchCodeFixProvider>
    {
        private const string test = @"
    using System;
    using System.Text.RegularExpressions;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                Regex usedRegex = new Regex(@""\p{ Sc}+\s *\d + "");
                usedRegex.IsMatch(""$ 5.60"");
            }
        }
    }";

        [Fact]
        public async Task CreatesDiagnosticsWhenDeclaringALocalRegexAndUsingIsMatch()
        {
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UseStaticRegexIsMatch.ToDiagnosticId(),
                Message = "Using static Regex.IsMatch can give better performance",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task IgnoresIsMatchCallFromStaticRegexClass()
        {
            const string testStatic = @"
    using System;
    using System.Text.RegularExpressions;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                Regex.IsMatch(""$ 5.60"", (@""\p{ Sc}+\s *\d + ""));
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(testStatic);
        }

        [Fact]
        public async Task WhenMakeMethodCallStatic()
        {

            const string fixtest = @"
    using System;
    using System.Text.RegularExpressions;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                Regex.IsMatch(""$ 5.60"", @""\p{ Sc}+\s *\d + "");
            }
        }
    }";
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task WhenMakeRegexCompiled()
        {

            const string fixtest = @"
    using System;
    using System.Text.RegularExpressions;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                Regex usedRegex = new Regex(@""\p{ Sc}+\s *\d + "", RegexOptions.Compiled);
                usedRegex.IsMatch(""$ 5.60"");
            }
        }
    }";
            await VerifyCSharpFixAsync(test, fixtest, 1);
        }

        [Fact]
        public async Task WhenMakeRegexCompiledAndStatic()
        {

            const string fixtest = @"
    using System;
    using System.Text.RegularExpressions;

    namespace ConsoleApplication1
    {
        class Program
        {
            public async Task Foo()
            {
                Regex.IsMatch(""$ 5.60"", @""\p{ Sc}+\s *\d + "", RegexOptions.Compiled);
            }
        }
    }";
            await VerifyCSharpFixAsync(test, fixtest, 2);
        }
    }
}