using CodeCracker.Refactoring;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Refactoring
{
    public class InvertForTests :
        CodeFixTest<InvertForAnalyzer, InvertForCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresWhenForUsesMoreThanOneIncrementor()
        {
            var test = _(@"for (var i = 0; i < n; i++, j++){}");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenForHasNoIncrementors()
        {
            var test = _(@"for (var i = 0; i < n; ){}");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenNotUsingPostfixIncrementOrDecrement()
        {
            var test = _(@"
            for (var i = 0; i < n; i+=1){}
            for (var i = 0; i < n; i-=1){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenUsingAnIncompatibleCondition()
        {
            var test = _(@"
            for (var i = 0; true; i+=1){}
            for (var i = 0; i >= n; i++){}
            for (var i = 0; i < n; i--){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenUsingMoreThanOneDeclaration()
        {
            var test = _(@"
            for (var i = 0, j = 2; i < n; i++){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenUsingNoDeclaration()
        {
            var test = _(@"
            int i = 0;
            for (; i < n; i++){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresIfDeclarationConditionOrIncrementorUsesADifferentVariable()
        {
            var test = _(@"
            for (var i = 0; i < n; j++){}
            for (var i = 0; j < n; i++){}
            for (var j = 0; i < n; i++){}
");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task FixReplacesFor0ToNWithNTo0()
        {
            var test = _(@"for (var i = 0; i < n; i++){}");
            var fixtest = _(@"for (var i = n - 1; i >= 0; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesFor0ToNWithNTo0WithDeclaredVariables()
        {
            var test = _(@"int i; for (i = 0; i < n; i++){}");
            var fixtest = _(@"int i; for (i = n - 1; i >= 0; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesForNTo0With0ToNWithDeclaredVariables()
        {
            var test = _(@"int i; for (i = n - 1; i >= 0; i--){}");
            var fixtest = _(@"int i; for (i = 0; i < n; i++){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesFor0ToNWithNTo0WithExplicitTyping()
        {
            var test = _(@"for (int i = 0; i < n; i++){}");
            var fixtest = _(@"for (int i = n - 1; i >= 0; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesForAToBWithBToA()
        {
            var test = _(@"for (var i = a; i < b; i++){}");
            var fixtest = _(@"for (var i = b - 1; i >= a; i--){}");
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixReplacesForNTo0With0ToN()
        {
            var test = _(@"for (var i = n - 1; i >= 0; i--){}");
            var fixtest = _(@"for (var i = 0; i < n; i++){}"); 
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task CreateDiagnosticsWithForLoopsFrom0ToN()
        {
            var test = _(@"for (var i = 0; i < n; i++){}");

            var expected = new DiagnosticResult
            {
                Id = InvertForAnalyzer.DiagnosticId,
                Message = "Make it a for loop that decrement the counter.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWithForLoopsTheUsesAnDeclaredVariableAsCounter()
        {
            var test = _(@"int i = 0; for (i = 0; i < n; i++){}");

            var expected = new DiagnosticResult
            {
                Id = InvertForAnalyzer.DiagnosticId,
                Message = "Make it a for loop that decrement the counter.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 28) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWithForLoopsFromNTo0()
        {
            var test = _(@"for (var i = n - 1; i >= 0; i--){}");

            var expected = new DiagnosticResult
            {
                Id = InvertForAnalyzer.DiagnosticId,
                Message = "Make it a for loop that increment the counter.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        public string _(string code)
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
