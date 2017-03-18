﻿using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class RethrowExceptionTests : CodeFixVerifier<RethrowExceptionAnalyzer, RethrowExceptionCodeFixProvider>
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
        public async Task WhenThrowingOriginalExceptionAnalyzerCreatesDiagnostic()
        {
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.RethrowException.ToDiagnosticId(),
                Message = "Throwing the same exception that was caught will loose the original stack trace.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 21) }
            };

            await VerifyCSharpDiagnosticAsync(sourceWithUsingSystem, expected);
        }

        [Fact]
        public async Task WhenThrowingOriginalExceptionAndApplyingThrowNewExceptionFix()
        {

            const string fixtest = @"
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
            await VerifyCSharpFixAsync(sourceWithUsingSystem, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingOriginalExceptionAndApplyingRethrowFix()
        {
            const string fixtest = @"
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
            await VerifyCSharpFixAsync(sourceWithUsingSystem, fixtest, 1, allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public async Task WhenThrowingOriginalExceptionAndApplyingThrowNewExceptionCompleteExceptionDeclationFix()
        {

            const string fixtest = @"
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
            await VerifyCSharpFixAsync(sourceWithoutUsingSystem, fixtest, 0);
        }

        [Fact]
        public async Task WhenThrowingExceptionOutsideAnyCatchBlock()
        {

            const string fixtest = @"
                namespace ConsoleApplication1
                {
                    class TypeName
                    {
                        public async Task Foo()
                        {
                            var ex = new ArgumentException(""An Exception"");
                            throw ex;
                        }
                    }
                }";
            await VerifyCSharpHasNoDiagnosticsAsync(fixtest);
        }
    }
}
