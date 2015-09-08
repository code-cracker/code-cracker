﻿using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class SplitIntoNestedIfTests : CodeFixVerifier<SplitIntoNestedIfAnalyzer, SplitIntoNestedIfCodeFixProvider>
    {
        [Fact]
        public async Task IfWithoutAndDoesNotCreateDiagnostic()
        {
            var source = "if (true) { }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IfWithBitwiseAndDoesNotCreateDiagnostic()
        {
            var source = "if (true & true) { }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IfWithElseDoesNotCreateDiagnostic()
        {
            var source = "if (true && true) { } else { }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IfWithAndCreatesDiagnostic()
        {
            var source = "if (true && true) { }".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.SplitIntoNestedIf.ToDiagnosticId(),
                Message = string.Format(SplitIntoNestedIfAnalyzer.Message),
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] {new DiagnosticResultLocation("Test0.cs", 10, 21)}
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IfWithOrDoesNotCreateDiagnostic()
        {
            var source = "if (true || true) { }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task GenerateNestedIfs()
        {
            var source = @"
                var cond1 = System.DateTime.Now.Second > 1;
                var cond2 = System.DateTime.Now.Second > 5;
                if (cond1 && cond2)
                {
                    System.Console.Write(1);
                }".WrapInCSharpMethod();
            var expected = @"
                var cond1 = System.DateTime.Now.Second > 1;
                var cond2 = System.DateTime.Now.Second > 5;
                if (cond1)
                {
                    if (cond2)
                    {
                        System.Console.Write(1);
                    }
                }".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task GenerateNestedIfsWithoutBlock()
        {
            var source = @"
                var cond1 = System.DateTime.Now.Second > 1;
                var cond2 = System.DateTime.Now.Second > 5;
                if (cond1 && cond2)
                    System.Console.Write(1);".WrapInCSharpMethod();
            var expected = @"
                var cond1 = System.DateTime.Now.Second > 1;
                var cond2 = System.DateTime.Now.Second > 5;
                if (cond1)
                    if (cond2)
                        System.Console.Write(1);".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FixAllInTheSameDocument()
        {
            var source = @"
                var cond1 = System.DateTime.Now.Second > 1;
                var cond2 = System.DateTime.Now.Second > 5;
                var cond3 = cond1;
                var cond4 = cond2;
                if (cond1 && cond2)
                {
                    if (cond3 && cond4)
                    {
                        System.Console.Write(1);
                    }
                }".WrapInCSharpMethod();
            var expected = @"
                var cond1 = System.DateTime.Now.Second > 1;
                var cond2 = System.DateTime.Now.Second > 5;
                var cond3 = cond1;
                var cond4 = cond2;
                if (cond1)
                {
                    if (cond2)
                    {
                        if (cond3)
                        {
                            if (cond4)
                            {
                                System.Console.Write(1);
                            }
                        }
                    }
                }".WrapInCSharpMethod();
            await VerifyFixAllAsync(source, expected);
        }
    }
}