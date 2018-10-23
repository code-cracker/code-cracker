using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class RemoveRedundantElseClauseTests : CodeFixVerifier<RemoveRedundantElseClauseAnalyzer, RemoveRedundantElseClauseCodeFixProvider>
    {
        [Fact]
        public async Task FixEmptyElse()
        {
            var test = @"
            if (1 == 2)
            {
            return 1;
            }
            else
            {
            }".WrapInCSharpMethod();
            var fixtest = @"
            if (1 == 2)
            {
            return 1;
            }".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task FixEmptyElseWhenThereIsNoSignificantCodeInsideItsBlock()
        {
            var test = @"
            if (1 == 2)
            {
            return 1;
            }
            else
            {
            //var a = 2;
            //var b = 3;
            //return b;
            }".WrapInCSharpMethod();
            var fixtest = @"
            if (1 == 2)
            {
            return 1;
            }".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest, 0);
        }

        [Fact]
        public async Task IgnoreWhenThereIsNoElse()
        {
            var test = @"
            if (1 == 2)
            {
                var a = 2;
            }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreSingleStatementElse()
        {
            var test = @"
            if (System.DateTime.Now.Second > 5)
            {
                var a = 2;
                a++;
            }
            else
                System.Diagnostics.Debug.WriteLine("""");".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreElseIf()
        {
            if (System.DateTime.Now.Second > 5)
            {
                var a = 2;
                a++;
            }
            else if (System.DateTime.Now.Second > 10)
            {
                var b = 3;
                b++;
            }
            var test = @"
            if (System.DateTime.Now.Second > 5)
            {
                var a = 2;
                a++;
            }
            else if (System.DateTime.Now.Second > 10)
            {
                var b = 3;
                b++;
            }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreWhenThereIsNoSignificantCodeInsideIfBlock()
        {
            var test = @"
            if (1 == 2)
            {
                //var a = 2;
                //return 1;
            }
            else
            {
            }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenEmptyElse()
        {
            var test = @"if(1 == 2){ return 1; } else { }".WrapInCSharpMethod();

            var expected = new DiagnosticResult(DiagnosticId.RemoveRedundantElseClause.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 37)
                .WithMessage("Remove redundant else");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CreateDiagnosticsWhenEmptyElseWithoutBlockOnIf()
        {
            var test = @"if(1 == 2) return 1; else { }".WrapInCSharpMethod();

            var expected = new DiagnosticResult(DiagnosticId.RemoveRedundantElseClause.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 34)
                .WithMessage("Remove redundant else");

            await VerifyCSharpDiagnosticAsync(test, expected);
        }
    }
}