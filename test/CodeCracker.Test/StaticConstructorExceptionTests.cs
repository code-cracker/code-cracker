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
            var test = @"
                public class MyClass 
                { 
                    static MyClass() 
                    { 
                        throw new System.Exception(""error message""); 
                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = StaticConstructorExceptionAnalyzer.DiagnosticId,
                Message = "Don't throw exception inside static constructors.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 25) }
            };

            VerifyCSharpDiagnostic(test, expected);
        }

        [Fact]
        public void NotWarningWhenNoExceptionIsThrowInsideStaticConstructor()
        {
            var test = @"
                public class MyClass 
                { 
                    public MyClass() 
                    { 
                        throw new System.Exception(""error message""); 
                    } 
                }";

            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void StaticConstructorWithoutException()
        {
            var test = @"
                public class MyClass 
                { 
                    static MyClass() 
                    { 

                    } 
                }";

            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void InstanceConstructorWithoutException()
        {
            var test = @"
                public class MyClass 
                { 
                    public MyClass() 
                    { 
                    
                    } 
                }";

            VerifyCSharpHasNoDiagnostics(test);
        }
    }
}