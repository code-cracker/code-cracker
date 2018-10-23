using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class InvertForTests : CodeFixVerifier<InvertForAnalyzer, InvertForCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresWhenForUsesMoreThanOneIncrementor()
        {
            var test = WrapInCSharpMethod(@"for (var i = 0; i < n; i++, j++){}");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenForHasNoIncrementors()
        {
            var test = WrapInCSharpMethod(@"for (var i = 0; i < n; ){}");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenNotUsingPostfixIncrementOrDecrement()
        {
            var test = WrapInCSharpMethod(@"
            for (var i = 0; i < n; i+=1){}
            for (var i = 0; i < n; i-=1){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenUsingAnIncompatibleCondition()
        {
            var test = WrapInCSharpMethod(@"
            for (var i = 0; true; i+=1){}
            for (var i = 0; i >= n; i++){}
            for (var i = 0; i < n; i--){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenUsingMoreThanOneDeclaration()
        {
            var test = WrapInCSharpMethod(@"
            for (var i = 0, j = 2; i < n; i++){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenUsingNoDeclaration()
        {
            var test = WrapInCSharpMethod(@"
            int i = 0;
            for (; i < n; i++){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresIfDeclarationConditionOrIncrementorUsesADifferentVariable()
        {
            var test = WrapInCSharpMethod(@"
            for (var i = 0; i < n; j++){}
            for (var i = 0; j < n; i++){}
            for (var j = 0; i < n; i++){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task FixReplacesFor0ToNWithNTo0()
        {
            var test = WrapInCSharpMethod(@"for (var i = 0; i < n; i++){}");
            var fixtest = WrapInCSharpMethod(@"for (var i = n - 1; i >= 0; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesFor0ToNWithNTo0WithDeclaredVariables()
        {
            var test = WrapInCSharpMethod(@"int i; for (i = 0; i < n; i++){}");
            var fixtest = WrapInCSharpMethod(@"int i; for (i = n - 1; i >= 0; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesForNTo0With0ToNWithDeclaredVariables()
        {
            var test = WrapInCSharpMethod(@"int i; for (i = n - 1; i >= 0; i--){}");
            var fixtest = WrapInCSharpMethod(@"int i; for (i = 0; i < n; i++){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesFor0ToNWithNTo0WithExplicitTyping()
        {
            var test = WrapInCSharpMethod(@"for (int i = 0; i < n; i++){}");
            var fixtest = WrapInCSharpMethod(@"for (int i = n - 1; i >= 0; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesForAToBWithBToA()
        {
            var test = WrapInCSharpMethod(@"for (var i = a; i < b; i++){}");
            var fixtest = WrapInCSharpMethod(@"for (var i = b - 1; i >= a; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesForNTo0With0ToN()
        {
            var test = WrapInCSharpMethod(@"for (var i = n - 1; i >= 0; i--){}");
            var fixtest = WrapInCSharpMethod(@"for (var i = 0; i < n; i++){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task CreateDiagnosticsWithForLoopsFrom0ToN()
        {
            var test = WrapInCSharpMethod(@"for (var i = 0; i < n; i++){}");

            var expected = new DiagnosticResult(DiagnosticId.InvertFor.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 17)
                .WithMessage("Make it a for loop that decrement the counter.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWithForLoopsTheUsesAnDeclaredVariableAsCounter()
        {
            var test = WrapInCSharpMethod(@"int i = 0; for (i = 0; i < n; i++){}");

            var expected = new DiagnosticResult(DiagnosticId.InvertFor.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 28)
                .WithMessage("Make it a for loop that decrement the counter.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWithForLoopsFromNTo0()
        {
            var test = WrapInCSharpMethod(@"for (var i = n - 1; i >= 0; i--){}");

            var expected = new DiagnosticResult(DiagnosticId.InvertFor.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 17)
                .WithMessage("Make it a for loop that increment the counter.");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        private static string WrapInCSharpMethod(string code)
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