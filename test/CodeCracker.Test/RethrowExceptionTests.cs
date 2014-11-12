using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class RethrowExceptionTests : CodeFixVerifier
    {
        private const string sourceWithoutUsingSystem = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                try { }
                catch (System.Exception ex)
                {
                    throw ex;
                }
            }
        }
    }";
        private const string sourceWithUsingSystem = "\n    using System;" + sourceWithoutUsingSystem;

        [Fact]
        public void WhenThrowingOriginalExceptionAnalyzerCreatesDiagnostic()
        {
            var expected = new DiagnosticResult
            {
                Id = RethrowExceptionAnalyzer.DiagnosticId,
                Message = "Don't throw the same exception you caught, you lose the original stack trace.",
                Severity = DiagnosticSeverity.Warning,
                Locations =
                    new[] {
                            new DiagnosticResultLocation("Test0.cs", 12, 21)
                        }
            };

            VerifyCSharpDiagnostic(sourceWithUsingSystem, expected);
        }

        [Fact]
        public void WhenThrowingOriginalExceptionAndApplyingThrowNewExceptionFix()
        {

            var fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                try { }
                catch (System.Exception ex)
                {
                    throw new Exception(""some reason to rethrow"", ex);
                }
            }
        }
    }";
            VerifyCSharpFix(sourceWithUsingSystem, fixtest, 0);
        }

        [Fact]
        public void WhenThrowingOriginalExceptionAndApplyingRethrowFix()
        {
            var fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                try { }
                catch (System.Exception ex)
                {
                    throw;
                }
            }
        }
    }";
            VerifyCSharpFix(sourceWithUsingSystem, fixtest, 1);
        }

        [Fact]
        public void WhenThrowingOriginalExceptionAndApplyingThrowNewExceptionCompleteExceptionDeclationFix()
        {

            var fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                try { }
                catch (System.Exception ex)
                {
                    throw new System.Exception(""some reason to rethrow"", ex);
                }
            }
        }
    }";
            VerifyCSharpFix(sourceWithoutUsingSystem, fixtest, 0);
        }


        protected override CodeFixProvider GetCSharpCodeFixProvider()
        {
            return new RethrowExceptionCodeFixProvider();
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new RethrowExceptionAnalyzer();
        }
    }
}