using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.CSharp.Test.Refactoring
{
    public class InvertForTests : CodeFixTest<InvertForAnalyzer, InvertForCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresWhenForUsesMoreThanOneIncrementor()
        {
            var test = WrapInMethod(@"for (var i = 0; i < n; i++, j++){}");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenForHasNoIncrementors()
        {
            var test = WrapInMethod(@"for (var i = 0; i < n; ){}");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenNotUsingPostfixIncrementOrDecrement()
        {
            var test = WrapInMethod(@"
            for (var i = 0; i < n; i+=1){}
            for (var i = 0; i < n; i-=1){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenUsingAnIncompatibleCondition()
        {
            var test = WrapInMethod(@"
            for (var i = 0; true; i+=1){}
            for (var i = 0; i >= n; i++){}
            for (var i = 0; i < n; i--){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenUsingMoreThanOneDeclaration()
        {
            var test = WrapInMethod(@"
            for (var i = 0, j = 2; i < n; i++){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenUsingNoDeclaration()
        {
            var test = WrapInMethod(@"
            int i = 0;
            for (; i < n; i++){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresIfDeclarationConditionOrIncrementorUsesADifferentVariable()
        {
            var test = WrapInMethod(@"
            for (var i = 0; i < n; j++){}
            for (var i = 0; j < n; i++){}
            for (var j = 0; i < n; i++){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task FixReplacesFor0ToNWithNTo0()
        {
            var test = WrapInMethod(@"for (var i = 0; i < n; i++){}");
            var fixtest = WrapInMethod(@"for (var i = n - 1; i >= 0; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesFor0ToNWithNTo0WithDeclaredVariables()
        {
            var test = WrapInMethod(@"int i; for (i = 0; i < n; i++){}");
            var fixtest = WrapInMethod(@"int i; for (i = n - 1; i >= 0; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesForNTo0With0ToNWithDeclaredVariables()
        {
            var test = WrapInMethod(@"int i; for (i = n - 1; i >= 0; i--){}");
            var fixtest = WrapInMethod(@"int i; for (i = 0; i < n; i++){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesFor0ToNWithNTo0WithExplicitTyping()
        {
            var test = WrapInMethod(@"for (int i = 0; i < n; i++){}");
            var fixtest = WrapInMethod(@"for (int i = n - 1; i >= 0; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesForAToBWithBToA()
        {
            var test = WrapInMethod(@"for (var i = a; i < b; i++){}");
            var fixtest = WrapInMethod(@"for (var i = b - 1; i >= a; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesForNTo0With0ToN()
        {
            var test = WrapInMethod(@"for (var i = n - 1; i >= 0; i--){}");
            var fixtest = WrapInMethod(@"for (var i = 0; i < n; i++){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task CreateDiagnosticsWithForLoopsFrom0ToN()
        {
            var test = WrapInMethod(@"for (var i = 0; i < n; i++){}");

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.InvertFor.ToDiagnosticId(),
                Message = "Make it a for loop that decrement the counter.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWithForLoopsTheUsesAnDeclaredVariableAsCounter()
        {
            var test = WrapInMethod(@"int i = 0; for (i = 0; i < n; i++){}");

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.InvertFor.ToDiagnosticId(),
                Message = "Make it a for loop that decrement the counter.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 28) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWithForLoopsFromNTo0()
        {
            var test = WrapInMethod(@"for (var i = n - 1; i >= 0; i--){}");

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.InvertFor.ToDiagnosticId(),
                Message = "Make it a for loop that increment the counter.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        private string WrapInMethod(string code)
        {
            return @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                " + code + @"
            }
        }
    }";
        }
    }
}