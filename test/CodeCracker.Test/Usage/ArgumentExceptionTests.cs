using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Usage
{
    public class ArgumentExceptionTests : CodeFixTest<ArgumentExceptionAnalyzer, ArgumentExceptionCodeFixProvider>
    {
        private const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }
        }
    }";

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAnalyzerCreatesDiagnostic()
        {
            var expected = new DiagnosticResult
            {
                Id = ArgumentExceptionAnalyzer.DiagnosticId,
                Message = "Type argument 'c' is not in the argument list.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 56) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingFirstFixUsesFirstParameter()
        {
            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""a"");
            }
        }
    }";
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingSecondFixUsesSecondParameter()
        {
            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""b"");
            }
        }
    }";
            await VerifyCSharpFixAsync(test, fixtest, 1);
        }
    }
}