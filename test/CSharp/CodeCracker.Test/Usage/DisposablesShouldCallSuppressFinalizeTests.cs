using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class DisposablesShouldCallSuppressFinalizeTests : CodeFixVerifier<DisposablesShouldCallSuppressFinalizeAnalyzer, DisposablesShouldCallSuppressFinalizeCodeFixProvider>
    {
        [Fact]
        public async void AlreadyCallsSuppressFinalizeWithArrowMethod()
        {
            const string source = @"
                public class MyType : System.IDisposable
                {
                    public void Dispose() => System.GC.SuppressFinalize(this);
                }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void AlreadyCallsSuppressFinalize()
        {
            const string source = @"
                public class MyType : System.IDisposable
                {
                    public void Dispose()
                    {
                        System.GC.SuppressFinalize(this);
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

            var expected = new DiagnosticResult(DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(4, 33)
                .WithMessage("'MyType' should call GC.SuppressFinalize inside the Dispose method.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async void DoNotWarnIfClassImplementsIDisposableWithSuppressFinalizeCallInFinally()
        {
            const string test = @"
                 public class MyType : System.IDisposable
                 {
                     public void Dispose()
                     {
                         try
                         {
                         }
                         finally
                         {
                             System.GC.SuppressFinalize(this);
                         }
                     }
                 }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void DoNotWarnIfClassImplementsIDisposableWithSuppressFinalizeCallInIf()
        {
            const string test = @"
                 public class MyType : System.IDisposable
                 {
                     public void Dispose()
                     {
                         if (true)
                         {
                             System.GC.SuppressFinalize(this);
                         }
                     }
                 }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void DoNotWarnIfClassImplementsIDisposableWithSuppressFinalizeCallInElse()
        {
            const string test = @"
                 public class MyType : System.IDisposable
                 {
                     public void Dispose()
                     {
                         if (true)
                         {
                         }
                         else
                         {
                             System.GC.SuppressFinalize(this);
                         }
                     }
                 }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NoWarningIfClassImplementsDisposableCallsSuppressFinalizeAndCallsDisposeWithThis()
        {
            const string source = @"
            using System;
            public class MyType : System.IDisposable
            {
                public void Dispose()
                {
                    this.Dispose(true);
                    GC.SuppressFinalize(this);
                }
            }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoWarningIfClassImplementsDisposableCallsSuppressFinalize()
        {
            const string source = @"
            using System;
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
        public async void NoWarningIfClassImplmentsIDisposableButDoesNotContainsAPublicConstructor()
        {
            const string test = @"
                public class MyType : System.IDisposable
                {
                    private MyType()
                    {
                    }

                    public void Dispose()
                    {
                    }

                    ~MyType() {}
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async void NoWarningIfClassIsAPrivateNestedType()
        {
            const string test = @"
                public class MyType
                {
                    private class MyNestedType : System.IDisposable
                    {
                        public void Dispose()
                        {
                        }

                        ~MyType() {}
                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void NoWarningIfClassIsNestedOfAPrivateNestedType()
        {
            const string test = @"
                public class MyType
                {
                    private class MyType
                    {
                        public class MyNestedType : System.IDisposable
                        {
                            public void Dispose()
                            {
                            }

                            ~MyType() {}
                        }
                    }
                }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
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

            var expected = new DiagnosticResult(DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(4, 33)
                .WithMessage("'MyType' should call GC.SuppressFinalize inside the Dispose method.");

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
                    using System;
                    public class MyType : System.IDisposable
                    {
                        public void Dispose()
                        {
                        }
                    }";

            const string fixtest = @"
                    using System;
                    public class MyType : System.IDisposable
                    {
                        public void Dispose()
                        {
                            GC.SuppressFinalize(this);
                        }
                    }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void WhenClassHasParametrizedDisposeMethod()
        {
            const string source = @"
                    using System;
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
                    using System;
                    public class MyType : System.IDisposable
                    {
                        public void Dispose()
                        {
                            Dispose(true);
                            GC.SuppressFinalize(this);
                        }
                        protected virtual void Dispose(bool disposing)
                        {
                        }
                    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void WhenClassExplicitImplementsOfIDisposableCallSuppressFinalize()
        {
            const string source = @"
                    using System;
                    public class MyType : IDisposable
                    {
                        void IDisposable.Dispose()
                        {
                        }
                    }";

            const string fixtest = @"
                    using System;
                    public class MyType : IDisposable
                    {
                        void IDisposable.Dispose()
                        {
                            GC.SuppressFinalize(this);
                        }
                    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void WhenClassHasParametrizedDisposeMethodAndExplicitlyImplementsIDisposable()
        {
            const string source = @"
                    using System;
                    public class MyType : System.IDisposable
                    {
                        void IDisposable.Dispose()
                        {
                            Dispose(true);
                        }

                        protected virtual void Dispose(bool disposing)
                        {

                        }
                    }";

            const string fixtest = @"
                    using System;
                    public class MyType : System.IDisposable
                    {
                        void IDisposable.Dispose()
                        {
                            Dispose(true);
                            GC.SuppressFinalize(this);
                        }

                        protected virtual void Dispose(bool disposing)
                        {

                        }
                    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void AddsSystemGCWhenSystemIsNotImported()
        {
            const string source = @"
                    public class MyType : System.IDisposable
                    {
                        void IDisposable.Dispose()
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
                        void IDisposable.Dispose()
                        {
                            Dispose(true);
                            System.GC.SuppressFinalize(this);
                        }
                        protected virtual void Dispose(bool disposing)
                        {

                        }
                    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void CallingSystemGCSupressFinalizeShouldNotGenerateDiags()
        {
            const string source = @"
                    public class MyType : System.IDisposable
                    {
                        void IDisposable.Dispose()
                        {
                            Dispose(true);
                            System.GC.SuppressFinalize(this);
                        }
                        protected virtual void Dispose(bool disposing)
                        {

                        }
                    }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void CallingGCSupressFinalizeWithAliasShouldNotGenerateDiags()
        {
            const string source = @"using A = System;
                    public class MyType : System.IDisposable
                    {
                        void IDisposable.Dispose()
                        {
                            Dispose(true);
                            A.GC.SuppressFinalize(this);
                        }
                        protected virtual void Dispose(bool disposing)
                        {

                        }
                    }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void UseSystemGCWhenSystemNamespaceWasNotImportedInCurrentContext()
        {
            const string source = @"
                    namespace A
                    {
                        using System;
                    }
                    namespace B
                    {
                        class Foo : System.IDisposable
                        {
                            public void Dispose()
                            {
                            }
                        }
                    }";

            const string fixtest = @"
                    namespace A
                    {
                        using System;
                    }
                    namespace B
                    {
                        class Foo : System.IDisposable
                        {
                            public void Dispose()
                            {
                                System.GC.SuppressFinalize(this);
                            }
                        }
                    }";


            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async void CallSupressWhenUsingExpressionBodiedMethod()
        {
            const string source = @"
                   using System;
                   using System.IO;

                   public class MyType : System.IDisposable
                   {
                        MemoryStream memory;

                        public virtual void Dispose() => memory.Dispose();
                   }";

            const string fixtest = @"
                   using System;
                   using System.IO;

                   public class MyType : System.IDisposable
                   {
                        MemoryStream memory;

                        public virtual void Dispose()
                        {
                           memory.Dispose();
                           GC.SuppressFinalize(this);
                        }
                   }";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

    }
}