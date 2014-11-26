using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class StaticConstructorExceptionTests : CodeFixTest<StaticConstructorExceptionAnalyzer, StaticConstructorExceptionCodeFixProvider>
    {
        [Fact]
        public async Task WarningIfExceptionIsThrowInsideStaticConstructor()
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

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task NotWarningWhenNoExceptionIsThrowInsideStaticConstructor()
        {
            var test = @"
                public class MyClass 
                { 
                    public MyClass() 
                    { 
                        throw new System.Exception(""error message""); 
                    } 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task StaticConstructorWithoutException()
        {
            var test = @"
                public class MyClass 
                { 
                    static MyClass() 
                    { 

                    } 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task InstanceConstructorWithoutException()
        {
            var test = @"
                public class MyClass 
                { 
                    public MyClass() 
                    { 
                    
                    } 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenThrowIsRemovedFromStaticConstructor()
        {
            var source = @"
                public class MyClass 
                { 
                    static MyClass() 
                    { 
                        throw new System.Exception(""error message""); 
                    } 
                }";

            var fixtest = @"
                public class MyClass 
                { 
                    static MyClass() 
                    { 
                    } 
                }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}