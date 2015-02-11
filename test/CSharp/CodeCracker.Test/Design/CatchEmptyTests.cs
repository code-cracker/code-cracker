using CodeCracker.CSharp.Design;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.CSharp.Test.Design
{
    public class CatchEmptyTests : CodeFixTest<CatchEmptyAnalyzer, CatchEmptyCodeFixProvider>
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
        public async Task WhenFindCatchEmptyThenPutExceptionClass()
        {
            const string test = @"
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

            const string fixtest = @"
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