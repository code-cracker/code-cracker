using CodeCracker.CSharp.Design;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    using Verify = CSharpCodeFixVerifier<EmptyCatchBlockAnalyzer, EmptyCatchBlockCodeFixProvider>;

    public class EmptyCatchBlockTests
    {
        readonly string test = @"
    using System;
    using System.Threading.Tasks;

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
                [|catch
                {
                }|]
            }
        }
    }";
        [Fact]
        public async Task EmptyCatchBlockAnalyzerCreateDiagnostic()
        {
            const string test = @"
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
                   throw;
                }
            }
        }
    }";
            await Verify.VerifyAnalyzerAsync(test);
        }

        [Fact]
        public async Task WhenRemoveTryCatchStatement()
        {

            const string fixtest = @"
    using System;
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                {
                   // do something
                }
            }
        }
    }";
            await Verify.VerifyCodeFixAsync(test, fixtest);
        }

        [Fact]
        public async Task WhenRemoveTryCatchStatementAndPutComment()
        {
            const string fixtest = @"
    using System;
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
            {
                // do something
            }
            //TODO: Consider reading MSDN Documentation about how to use Try...Catch => http://msdn.microsoft.com/en-us/library/0yd65esw.aspx
        }
        }
    }";

            await new Verify.Test
            {
                TestCode = test,
                FixedCode = fixtest,
                CodeFixIndex = 1,
            }.RunAsync();
        }

        [Fact]
        public async Task WhenPutExceptionClassInCatchBlock()
        {
            const string fixtest = @"
    using System;
    using System.Threading.Tasks;

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
                throw;
            }
        }
        }
    }";
            await new Verify.Test
            {
                TestCode = test,
                FixedCode = fixtest,
                CodeFixIndex = 2,
            }.RunAsync();
        }


        [Fact]
        public async Task WhenMultipleCatchOnlyRemoveSelected()
        {
            const string test = @"
using System;

namespace ConsoleApplication1
{
    class TypeName
    {
        public async {|CS0246:Task|} {|CS0161:{|CS1983:Foo|}|}()
        {
            int x;
            try
            {
                // do something
            }
            [|catch (System.ArgumentException ae)
            {
            }|]
            catch (System.Exception ex)
            {
                x = 1;
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
        public async {|CS0246:Task|} {|CS0161:{|CS1983:Foo|}|}()
        {
            int x;
            try
            {
                // do something
            }
            catch (System.Exception ex)
            {
                x = 1;
            }
        }
    }
}";
            await Verify.VerifyCodeFixAsync(test, fixtest);
        }
    }
}