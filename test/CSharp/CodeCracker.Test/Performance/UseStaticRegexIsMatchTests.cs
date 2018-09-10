using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

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
            var expected = new DiagnosticResult(DiagnosticId.UseStaticRegexIsMatch.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(12, 17)
                .WithMessage(UseStaticRegexIsMatchAnalyzer.MessageFormat);

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
            await VerifyCSharpFixAsync(test, fixtest, 0, allowNewCompilerDiagnostics: true); //todo: should not need to allow new compiler diagnostic, fix test infrastructure to understand the Regex type
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
            await VerifyCSharpFixAsync(test, fixtest, 2, allowNewCompilerDiagnostics: true); //todo: should not need to allow new compiler diagnostic, fix test infrastructure to understand the Regex type
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
            await VerifyCSharpFixAsync(test, fixtest, 1, allowNewCompilerDiagnostics: true); //todo: should not need to allow new compiler diagnostic, fix test infrastructure to understand the Regex type
        }

        [Fact]
        public async Task IgnoresIsMatchCallClassMember()
        {
            const string testStatic = @"
            public class RegexTestClass
            {
                private TestModel testModel;

                private void Test(string text)
                {
                    if (testModel.Regex.IsMatch(text))
                    {
                        return;
                    }
                }
            }

            public class TestModel
            {
                public Regex Regex { get; set; }
            }";
            await VerifyCSharpHasNoDiagnosticsAsync(testStatic);
        }

        [Fact]
        public async Task IgnoresIsMatchCallClassMemberInsideClass()
        {
            const string testStatic = @"
           public class RegexTestClass
            {
                private C c;

                private void Test(string text)
                {
                    if (c.TestModel.Regex.IsMatch(text))
                    {
                        return;
                    }
                }
            }
            public class C
            {
                public TestModel TestModel { get; set; }
            }
            public class TestModel
            {
                public System.Text.RegularExpressions.Regex Regex { get; set; }
            }";
            await VerifyCSharpHasNoDiagnosticsAsync(testStatic);
        }
    }
}