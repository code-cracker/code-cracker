using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class ArgumentExceptionTests : CodeFixTest<ArgumentExceptionAnalyzer, ArgumentExceptionCodeFixProvider>
    {
        private const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""c"");
            }
        }
    }";

        [Fact]
        public void WhenThrowingArgumentExceptionWithInvalidArgumentAnalyzerCreatesDiagnostic()
        {
            var expected = new DiagnosticResult
            {
                Id = ArgumentExceptionAnalyzer.DiagnosticId,
                Message = "Type argument 'c' is not in the argument list.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 56) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingFirstFixUsesFirstParameter()
        {
            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""a"");
            }
        }
    }";
            VerifyCSharpFix(test, fixtest, 0);
        }

        [Fact]
        public void WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingSecondFixUsesSecondParameter()
        {
            var fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(int a, int b)
            {
                throw new ArgumentException(""message"", ""b"");
            }
        }
    }";
            VerifyCSharpFix(test, fixtest, 1);
        }
    }
}