using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Usage
{
    public class DisposablesShouldCallSuppressFinalizeTests : CodeFixTest<DisposablesShouldCallSuppressFinalizeAnalyzer, DisposablesShouldCallSuppressFinalizeCodeFixProvider>
    {
        [Fact]
        public async void WarningIfStructImplmentsIDisposableWithNoSuppressFinalizeCall()
        {
            var test = @"
                public struct MyType : System.IDisposable
                { 
                    public void Dispose() 
                    { 
                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = DisposablesShouldCallSuppressFinalizeAnalyzer.DiagnosticId,
                Message = "'MyType' should call GC.SuppressFinalize inside the Dispose method.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 33) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void WarningIfClassImplmentsIDisposableWithNoSuppressFinalizeCall()
        {
            var test = @"
                public class MyType : System.IDisposable
                { 
                    public void Dispose() 
                    { 
                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = DisposablesShouldCallSuppressFinalizeAnalyzer.DiagnosticId,
                Message = "'MyType' should call GC.SuppressFinalize inside the Dispose method.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 33) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void NoWarningIfStructDoesNotImplementsIDisposable()
        {
            var test = @"
                public struct MyType
                { 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void NoWarningIfClassDoesNotImplementsIDisposable()
        {
            var test = @"
                public class MyType
                { 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void WhenStructImplementsIDisposableCallSuppressFinalize()
        {
            var source = @"
                    public struct MyType : System.IDisposable
                    { 
                        public void Dispose() 
                        { 
                            var x = 123;
                        } 
                    }";

            var fixtest = @"
                    public struct MyType : System.IDisposable
                    { 
                        public void Dispose() 
                        {
                            var x = 123;
                            GC.SuppressFinalize(this);
                        } 
                    }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void WhenClassImplementsIDisposableCallSuppressFinalize()
        {
            var source = @"
                    public class MyType : System.IDisposable
                    { 
                        public void Dispose() 
                        { 
                            var x = 123;
                        } 
                    }";

            var fixtest = @"
                    public class MyType : System.IDisposable
                    { 
                        public void Dispose() 
                        { 
                            var x = 123;
                            GC.SuppressFinalize(this);
                        } 
                    }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void WhenClassHasParametrizedDisposeMethod()
        {
            var source = @"
                    public class MyType : System.IDisposable
                    { 
                        public void Dispose() 
                        { 
                            Dispose(true);
                        } 

                        protected virtual void Dispose(bool disposing)
                        {
                            
                        }
                    }";

            var fixtest = @"
                    public class MyType : System.IDisposable
                    { 
                        public void Dispose() 
                        { 
                            Dispose(true);
                        } 

                        protected virtual void Dispose(bool disposing)
                        {
                            GC.SuppressFinalize(this);
                        }
                    }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}