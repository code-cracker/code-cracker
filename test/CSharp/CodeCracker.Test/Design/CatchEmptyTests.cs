using System.Threading.Tasks;
using CodeCracker.CSharp.Design;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    using Verify = CSharpCodeFixVerifier<CatchEmptyAnalyzer, CatchEmptyCodeFixProvider>;

    public class CatchEmptyTests
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
            public async {|CS0246:Task|} {|CS0161:{|CS1983:Foo|}|}()
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

            await Verify.VerifyAnalyzerAsync(source);
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
            public async {|CS0246:Task|} {|CS0161:{|CS1983:Foo|}|}()
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
            await Verify.VerifyAnalyzerAsync(source);
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
            await Verify.VerifyAnalyzerAsync(source);
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
                [|catch
                {
                    if (x == 1)
                        throw;
                    else
                        return;
                }|]
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
            await Verify.VerifyCodeFixAsync(test, fixtest);
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
                [|catch
                {
                   int x = 0;
                }|]
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
            await Verify.VerifyCodeFixAsync(test, fixtest);
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
                [|catch
                {
                   int x = 0;
                   return;
                }|]
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
            await Verify.VerifyCodeFixAsync(test, fixtest);
        }
    }
}