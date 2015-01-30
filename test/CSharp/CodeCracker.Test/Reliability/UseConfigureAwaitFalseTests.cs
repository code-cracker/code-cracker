using System.Threading.Tasks;
using CodeCracker.Reliability;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Reliability
{
    public class UseConfigureAwaitFalseTests : 
        CodeFixTest<UseConfigureAwaitFalseAnalyzer, UseConfigureAwaitFalseCodeFixProvider>
    {
        [Theory]
        [InlineData("System.Threading.Tasks.Task t; await t;", 48)]
        [InlineData("System.Threading.Tasks.Task t; await t.ContinueWith(_ => 42);", 48)]
        [InlineData("await System.Threading.Tasks.Task.Delay(1000);", 17)]
        [InlineData("await System.Threading.Tasks.Task.FromResult(0);", 17)]
        [InlineData("await System.Threading.Tasks.Task.Run(() => {});", 17)]
        [InlineData("Func<System.Threading.Tasks.Task> f; await f();", 54)]
        public async Task WhenAwaitingTaskAnalyzerCreatesDiagnostic(string sample, int column)
        {
            var test = sample.WrapInMethod(isAsync: true);

            var expected = new DiagnosticResult
            {
                Id = UseConfigureAwaitFalseAnalyzer.DiagnosticId,
                Message = "Consider using ConfigureAwait(false) on the awaited task.",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, column) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Theory]
        [InlineData("System.Threading.Tasks.Task t; await t.ConfigureAwait(false);")]
        [InlineData("System.Threading.Tasks.Task t; await t.ContinueWith(_ => 42).ConfigureAwait(false);")]
        [InlineData("await System.Threading.Tasks.Task.Delay(1000).ConfigureAwait(false);")]
        [InlineData("await System.Threading.Tasks.Task.FromResult(0).ConfigureAwait(false);")]
        [InlineData("await System.Threading.Tasks.Task.Run(() => {}).ConfigureAwait(false);")]
        [InlineData("Func<System.Threading.Tasks.Task> f; await f().ConfigureAwait(false);")]
        [InlineData("await System.Threading.Tasks.Task.Yield();")]
        [InlineData("await UnknownAsync();")]
        public async Task WhenAwaitingANonTaskNoDiagnosticIsCreated(string sample)
        {
            var test = sample.WrapInMethod(isAsync: true);
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
    }
}
