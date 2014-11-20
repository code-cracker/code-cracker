using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class StaticConstructorExceptionTests : CodeFixTest<StaticConstructorExceptionAnalyzer, StaticConstructorExceptionCodeFixProvider>
    {
        [Fact]
        public void WarningIfExceptionIsThrowInsideStaticConstructor()
        {
            var test = @"public class MyClass { static MyClass() { throw new System.Exception(""error message""); } }";

            var expected = new DiagnosticResult
            {
                Id = StaticConstructorExceptionAnalyzer.DiagnosticId,
                Message = "Don't throw exception inside static constructors.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 0, 43) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void NotWarningWhenNoExceptionIsThrowInsideStaticConstructor()
        {
            var test = @"public class MyClass { public MyClass() { throw new System.Exception(""error message""); } }";

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void StaticConstructorWithoutException()
        {
            var test = @"public class MyClass { static MyClass() { } }";

            VerifyCSharpDiagnostic(test);
        }

        [Fact]
        public void InstanceConstructorWithoutException()
        {
            var test = @"public class MyClass { static MyClass() { } }";

            VerifyCSharpDiagnostic(test);
        }
    }
}