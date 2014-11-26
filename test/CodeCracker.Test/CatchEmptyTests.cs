using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class CatchEmptyTests : CodeFixTest<CatchEmptyAnalyzer, CatchEmptyCodeFixProvider>
    {

        [Fact]
        public async Task CatchEmptyAnalyserCreateDiagnostic()
        {
            var source = @"
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
        public async Task WhenFindCatchEmptyThenPutExceptionClass()
        {
            var test = @"
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
                   int x = 0;
                }
            }
        }
    }";

            var fixtest = @"
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
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }
    }
}