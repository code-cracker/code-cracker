using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class RethrowExceptionTests : CodeFixTest<RethrowExceptionAnalyzer, RethrowExceptionCodeFixProvider>
    {
        private const string sourceWithoutUsingSystem = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
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
        public async Task WhenThrowingOriginalExceptionAnalyzerCreatesDiagnostic()
        {
            var expected = new DiagnosticResult
            {
                Id = RethrowExceptionAnalyzer.DiagnosticId,
                Message = "Don't throw the same exception you caught, you lose the original stack trace.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 21) }
            };

            await VerifyCSharpDiagnosticAsync(sourceWithUsingSystem, expected);
        }

        [Fact]
        public async Task WhenThrowingOriginalExceptionAndApplyingThrowNewExceptionFix()
        {

            var fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                try { }
                catch (System.Exception ex)
                {
                    throw new Exception(""some reason to rethrow"", ex);
                }
            }
        }
    }";
            await VerifyCSharpFixAsync(sourceWithUsingSystem, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingOriginalExceptionAndApplyingRethrowFix()
        {
            var fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                try { }
                catch (System.Exception ex)
                {
                    throw;
                }
            }
        }
    }";
            await VerifyCSharpFixAsync(sourceWithUsingSystem, fixtest, 1);
        }

        [Fact]
        public async Task WhenThrowingOriginalExceptionAndApplyingThrowNewExceptionCompleteExceptionDeclationFix()
        {

            var fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                try { }
                catch (System.Exception ex)
                {
                    throw new System.Exception(""some reason to rethrow"", ex);
                }
            }
        }
    }";
            await VerifyCSharpFixAsync(sourceWithoutUsingSystem, fixtest, 0);
        }
    }
}