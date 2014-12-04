﻿using CodeCracker.Design;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Design
{
    public class EmptyCatchBlockTests : CodeFixTest<EmptyCatchBlockAnalyzer, EmptyCatchBlockCodeFixProvider>
    {
        string test = @"
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
                }
            }
        }
    }";
        [Fact]
        public async Task EmptyCatchBlockAnalyzerCreateDiagnostic()
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

            var fixtest = @"
    using System;

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
            await VerifyCSharpFixAsync(test, fixtest, 0,false,true);
        }

        [Fact]
        public async Task WhenRemoveTryCatchStatementAndPutComment()
        {
            var fixtest = @"
    using System;

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

            await VerifyCSharpFixAsync(test, fixtest, 1, false, true);
        }

        [Fact]
        public async Task WhenPutExceptionClassInCatchBlock()
        {
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
                   throw;
                }
            }
        }
    }";

            await VerifyCSharpFixAsync(test, fixtest, 2, false, true);
        }
    }
}