using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class CatchEmptyTests : CodeFixTest<CatchEmptyAnalyzer, CatchEmptyCodeFixProvider>
    {

        [Fact]
        public void CatchEmptyAnalyserCreateDiagnostic()
        {
            var source = @"
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

            VerifyCSharpHasNoDiagnostics(source);
        }

        [Fact]
        public void WhenFindCatchEmptyThenPutExceptionClass()
        {
            var test = @"
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

            var fixtest = @"
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
            VerifyCSharpFix(test, fixtest, 0);
        }
    }
}