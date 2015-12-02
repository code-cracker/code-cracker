using CodeCracker.CSharp.Design;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class EmptyCatchBlockTests : CodeFixVerifier<EmptyCatchBlockAnalyzer, EmptyCatchBlockCodeFixProvider>
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
                catch
                {
                }
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
            await VerifyCSharpHasNoDiagnosticsAsync(test);
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
            await VerifyCSharpFixAsync(test, fixtest, 0, allowNewCompilerDiagnostics: false, formatBeforeCompare: true);
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

            await VerifyCSharpFixAsync(test, fixtest, 1, allowNewCompilerDiagnostics: false, formatBeforeCompare: true);
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
            await VerifyCSharpFixAsync(test, fixtest, 2, allowNewCompilerDiagnostics: true, formatBeforeCompare: true);
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
        public async Task Foo()
        {
            int x;
            try
            {
                // do something
            }
            catch (System.ArgumentException ae)
            {
            }
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
        public async Task Foo()
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
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }
    }
}