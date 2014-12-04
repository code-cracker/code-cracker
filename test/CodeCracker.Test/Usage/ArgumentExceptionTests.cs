using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Usage
{
    public class ArgumentExceptionTests : CodeFixTest<ArgumentExceptionAnalyzer, ArgumentExceptionCodeFixProvider>
    {
        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAnalyzerCreatesDiagnostic()
        {
            var test = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var expected = new DiagnosticResult
            {
                Id = ArgumentExceptionAnalyzer.DiagnosticId,
                Message = "Type argument 'c' is not in the argument list.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 56) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionInCtorWithInvalidArgumentAnalyzerCreatesDiagnostic()
        {
            var test = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var expected = new DiagnosticResult
            {
                Id = ArgumentExceptionAnalyzer.DiagnosticId,
                Message = "Type argument 'c' is not in the argument list.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 56) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingFirstFixUsesFirstParameter()
        {
            var test = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""a"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingSecondFixUsesSecondParameter()
        {
            var test = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = _(@"
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""b"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 1);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInCtorAndApplyingFirstFixUsesFirstParameter()
        {
            var test = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""a"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentInCtorAndApplyingSecondFixUsesSecondParameter()
        {
            var test = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }");

            var fixtest = _(@"
            public TypeName(int a, int b)
            {
                throw new ArgumentException(""message"", ""b"");
            }");
            await VerifyCSharpFixAsync(test, fixtest, 1);
        }


        static string _(string code)
        {
            return @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            " + code + @"
        }
    }";

        }
    }
}