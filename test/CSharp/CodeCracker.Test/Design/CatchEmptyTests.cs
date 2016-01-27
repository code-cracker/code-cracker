using CodeCracker.CSharp.Design;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class CatchEmptyTests : CodeFixVerifier<CatchEmptyAnalyzer, CatchEmptyCodeFixProvider>
    {

        [Fact]
        public async Task CatchEmptyAnalyserCreateDiagnostic()
        {
            const string source = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                try
                {
                   // do something
                }
                catch (Exception ex)
                {
                   int x = 0;
                }
            }
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task EmptyCatchEndsWithThrowNoDiagnostic()
        {
            const string source = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                try
                {
                    // do something
                }
                catch
                {
                    throw;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task EmptyCatchWithNestedThrowNoDiagnostic()
        {
            const string source = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            int x;
            public void Foo()
            {
                try
                {
                    // do something
                }
                catch
                {
                    if (x == 1)
                        throw;
                    else
                        throw;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
        [Fact]
        public async Task NotAllowedToReturnOutOfEmtpyCatchBlock()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            int x;
            public void Foo()
            {
                try
                {
                    // do something
                }
                catch
                {
                    if (x == 1)
                        throw;
                    else
                        return;
                }
            }
        }
    }";

            const string fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            int x;
            public void Foo()
            {
                try
                {
                   // do something
                }
                catch (Exception ex)
                {
                    if (x == 1)
                        throw;
                    else
                        return;
                }
            }
        }
    }";
            await VerifyCSharpFixAsync(test, fixtest, 0, allowNewCompilerDiagnostics: true);
        }
        [Fact]
        public async Task WhenFindCatchEmptyThenPutExceptionClass()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                try
                {
                   // do something
                }
                catch
                {
                   int x = 0;
                }
            }
        }
    }";

            const string fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                try
                {
                   // do something
                }
                catch (Exception ex)
                {
                   int x = 0;
                }
            }
        }
    }";
            await VerifyCSharpFixAsync(test, fixtest, 0, allowNewCompilerDiagnostics: true);
        }
        [Fact]
        public async Task AddCatchEvenIfThereIsReturnInBlock()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                try
                {
                   // do something
                }
                catch
                {
                   int x = 0;
                   return;
                }
            }
        }
    }";

            const string fixtest = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                try
                {
                   // do something
                }
                catch (Exception ex)
                {
                   int x = 0;
                   return;
                }
            }
        }
    }";
            await VerifyCSharpFixAsync(test, fixtest, 0, allowNewCompilerDiagnostics: true);
        }
    }
}