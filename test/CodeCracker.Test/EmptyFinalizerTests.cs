using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class EmptyFinalizerTests : CodeFixTest<EmptyFinalizerAnalyzer, EmptyFinalizerCodeFixProvider>
    {
        [Fact]
        public async Task RemoveEmptyFinalizerWhenIsEmpty()
        {
            var test = @"
                public class MyClass 
                { 
                    ~MyClass() 
                    { 

                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = EmptyFinalizerAnalyzer.DiagnosticId,
                Message = "Remove Empty Finalizers",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 21) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task RemoveEmptyFinalizerWithSingleLineComment()
        {
            var test = @"
                public class MyClass 
                { 
                    ~MyClass() 
                    { 
                        //comments...
                    }
                }";

            var expected = new DiagnosticResult
            {
                Id = EmptyFinalizerAnalyzer.DiagnosticId,
                Message = "Remove Empty Finalizers",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 21) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task RemoveEmptyFinalizerWithMultiLineComment()
        {
            var test = @"
                public class MyClass 
                { 
                    ~MyClass() 
                    { 
                        /*
                            multiline
                            comments
                        */
                    }
                }";

            var expected = new DiagnosticResult
            {
                Id = EmptyFinalizerAnalyzer.DiagnosticId,
                Message = "Remove Empty Finalizers",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 21) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task MaintainFinalizerWhenUsed()
        {
            var test = @"
                public class MyClass
                {
                    private System.IntPtr pointer;

                    ~MyClass() 
                    { 
                        pointer = System.IntPtr.Zero; 
                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenFinalizerIsRemovedFromClass()
        {
            var source = @"
                public class MyClass 
                { 
                    ~MyClass() 
                    { 

                    } 
                }";

            var fixtest = @"
                public class MyClass 
                { 
                }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}