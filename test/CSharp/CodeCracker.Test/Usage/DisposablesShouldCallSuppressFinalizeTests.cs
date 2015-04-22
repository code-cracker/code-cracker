using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class DisposablesShouldCallSuppressFinalizeTests : CodeFixVerifier<DisposablesShouldCallSuppressFinalizeAnalyzer, DisposablesShouldCallSuppressFinalizeCodeFixProvider>
    {

        [Fact]
        public async void AlreadyCallsSuppressFinalize()
        {
            const string source = @"
                public class MyType : System.IDisposable
                {
                    public void Dispose()
                    {
                        GC.SuppressFinalize(this);
                    }
                }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void DoNotWarnIfStructImplmentsIDisposableWithNoSuppressFinalizeCall()
        {
            const string test = @"
                public struct MyType : System.IDisposable
                { 
                    public void Dispose() 
                    { 
                    } 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void WarningIfClassImplmentsIDisposableWithNoSuppressFinalizeCall()
        {
            const string test = @"
                public class MyType : System.IDisposable
                { 
                    public void Dispose() 
                    { 
                    } 
                }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(),
                Message = "'MyType' should call GC.SuppressFinalize inside the Dispose method.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 33) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void NoWarningIfStructDoesNotImplementsIDisposable()
        {
            const string test = @"
                public struct MyType
                { 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void NoWarningIfClassIsSealedWithNoUserDefinedFinalizer()
        {
            const string test = @"
                public sealed class MyType : System.IDisposable
                { 
                    public void Dispose() 
                    { 
                    } 
                }"
                ;

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void WarningIfSealedClassHaveUserDefinedFinalizerImplmentsIDisposableWithNoSuppressFinalizeCall()
        {
            const string test = @"
                public sealed class MyType : System.IDisposable
                { 
                    public void Dispose() 
                    { 
                    }

                    ~MyType() {} 
                }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(),
                Message = "'MyType' should call GC.SuppressFinalize inside the Dispose method.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 33) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void NoWarningIfClassDoesNotImplementsIDisposable()
        {
            const string test = @"
                public class MyType 
                { 
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async void WhenClassImplementsIDisposableCallSuppressFinalize()
        {
            const string source = @"
                    public class MyType : System.IDisposable
                    { 
                        public void Dispose() 
                        { 
                            var x = 123;
                        } 
                    }";

            const string fixtest = @"
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
            const string source = @"
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

            const string fixtest = @"
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

        [Fact]
        public async void WhenClassExplicitImplementsOfIDisposableCallSuppressFinalize()
        {
            const string source = @"
                    public class MyType : System.IDisposable
                    { 
                        public void IDisposable.Dispose() 
                        { 
                            var x = 123;
                        } 
                    }";

            const string fixtest = @"
                    public class MyType : System.IDisposable
                    { 
                        public void IDisposable.Dispose() 
                        { 
                            var x = 123;
                            GC.SuppressFinalize(this);
                        } 
                    }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void WhenClassHasParametrizedDisposeMethodAndExplicitlyImplementsIDisposable()
        {
            const string source = @"
                    public class MyType : System.IDisposable
                    { 
                        public void IDisposable.Dispose() 
                        { 
                            Dispose(true);
                        } 

                        protected virtual void Dispose(bool disposing)
                        {
                            
                        }
                    }";

            const string fixtest = @"
                    public class MyType : System.IDisposable
                    { 
                        public void IDisposable.Dispose() 
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