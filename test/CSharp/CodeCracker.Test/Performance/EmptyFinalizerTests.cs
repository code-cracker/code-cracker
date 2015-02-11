using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class EmptyFinalizerTests : CodeFixVerifier<EmptyFinalizerAnalyzer, EmptyFinalizerCodeFixProvider>
    {
        [Fact]
        public async Task RemoveEmptyFinalizerWhenIsEmpty()
        {
            const string test = @"
                public class MyClass 
                { 
                    ~MyClass() 
                    { 

                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.EmptyFinalizer.ToDiagnosticId(),
                Message = "Remove Empty Finalizers",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 21) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task RemoveEmptyFinalizerWithSingleLineComment()
        {
            const string test = @"
                public class MyClass 
                { 
                    ~MyClass() 
                    { 
                        //comments...
                    }
                }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.EmptyFinalizer.ToDiagnosticId(),
                Message = "Remove Empty Finalizers",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 21) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task RemoveEmptyFinalizerWithMultiLineComment()
        {
            const string test = @"
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
                Id = DiagnosticId.EmptyFinalizer.ToDiagnosticId(),
                Message = "Remove Empty Finalizers",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 21) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task MaintainFinalizerWhenUsed()
        {
            const string test = @"
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
            const string source = @"
                public class MyClass 
                { 
                    ~MyClass() 
                    { 

                    } 
                }";

            const string fixtest = @"
                public class MyClass 
                { 
                }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}