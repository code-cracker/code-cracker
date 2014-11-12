﻿using System;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class CatchEmptyTests : CodeFixVerifier
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

        protected override CodeFixProvider GetBasicCodeFixProvider()
        {
            throw new NotImplementedException();
        }

        protected override DiagnosticAnalyzer GetBasicDiagnosticAnalyzer()
        {
            throw new NotImplementedException();
        }

        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new CatchEmptyCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new CatchEmptyAnalyser();
        }
    }
}