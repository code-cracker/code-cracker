using CodeCracker.CSharp.Reliability;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Reliability
{
    public class UseConfigureAwaitFalseTests : CodeFixVerifier<UseConfigureAwaitFalseAnalyzer, UseConfigureAwaitFalseCodeFixProvider>
    {
        [Theory]
        [InlineData("System.Threading.Tasks.Task t; await t;", 44)]
        [InlineData("System.Threading.Tasks.Task t; await t.ContinueWith(_ => 42);", 44)]
        [InlineData("await System.Threading.Tasks.Task.Delay(1000);", 13)]
        [InlineData("await System.Threading.Tasks.Task.FromResult(0);", 13)]
        [InlineData("await System.Threading.Tasks.Task.Run(() => {});", 13)]
        [InlineData("Func<System.Threading.Tasks.Task> f; await f();", 50)]
        public async Task WhenAwaitingTaskAnalyzerCreatesDiagnostic(string sample, int column)
        {
            var test = sample.WrapInCSharpMethod(isAsync: true);

            var expected = new DiagnosticResult(DiagnosticId.UseConfigureAwaitFalse.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, column)
                .WithMessage("Consider using ConfigureAwait(false) on the awaited task.");

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
            var test = sample.WrapInCSharpMethod(isAsync: true);
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Theory]
        [InlineData(
            "System.Threading.Tasks.Task t; await t;",
            "System.Threading.Tasks.Task t; await t.ConfigureAwait(false);")]
        [InlineData(
            "System.Threading.Tasks.Task t; await t.ContinueWith(_ => 42);",
            "System.Threading.Tasks.Task t; await t.ContinueWith(_ => 42).ConfigureAwait(false);")]
        [InlineData(
            "await System.Threading.Tasks.Task.Delay(1000);",
            "await System.Threading.Tasks.Task.Delay(1000).ConfigureAwait(false);")]
        [InlineData(
            "await System.Threading.Tasks.Task.FromResult(0);",
            "await System.Threading.Tasks.Task.FromResult(0).ConfigureAwait(false);")]
        [InlineData(
            "await System.Threading.Tasks.Task.Run(() => {});",
            "await System.Threading.Tasks.Task.Run(() => {}).ConfigureAwait(false);")]
        [InlineData(
            "Func<System.Threading.Tasks.Task> f; await f();",
            "Func<System.Threading.Tasks.Task> f; await f().ConfigureAwait(false);")]
        public async Task FixAddsConfigureAwaitFalse(string original, string result)
        {
            var test = original.WrapInCSharpMethod(isAsync: true);
            var fixtest = result.WrapInCSharpMethod(isAsync: true);

            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Theory]
        [InlineData(
            "System.Threading.Tasks.Task t; await t;",
            "System.Threading.Tasks.Task t; await t.ConfigureAwait(false);")]
        [InlineData(
            "System.Threading.Tasks.Task t; await t.ContinueWith(_ => 42); await t.ContinueWith(_ => 42);",
            "System.Threading.Tasks.Task t; await t.ContinueWith(_ => 42).ConfigureAwait(false); await t.ContinueWith(_ => 42).ConfigureAwait(false);")]
        [InlineData(
            "await System.Threading.Tasks.Task.Delay(1000);",
            "await System.Threading.Tasks.Task.Delay(1000).ConfigureAwait(false);")]
        [InlineData(
            "await System.Threading.Tasks.Task.FromResult(0);await System.Threading.Tasks.Task.FromResult(0);",
            "await System.Threading.Tasks.Task.FromResult(0).ConfigureAwait(false);await System.Threading.Tasks.Task.FromResult(0).ConfigureAwait(false);")]
        [InlineData(
            "await System.Threading.Tasks.Task.Run(() => {});",
            "await System.Threading.Tasks.Task.Run(() => {}).ConfigureAwait(false);")]
        [InlineData(
            "Func<System.Threading.Tasks.Task> f; await f();",
            "Func<System.Threading.Tasks.Task> f; await f().ConfigureAwait(false);")]
        public async Task FixAllAddsConfigureAwaitFalse(string original, string result)
        {
            var test1 = original.WrapInCSharpMethod(isAsync: true, typeName: "MyType1");
            var fixtest1 = result.WrapInCSharpMethod(isAsync: true, typeName: "MyType1");
            var test2 = original.WrapInCSharpMethod(isAsync: true, typeName: "MyType2");
            var fixtest2 = result.WrapInCSharpMethod(isAsync: true, typeName: "MyType2");

            await VerifyCSharpFixAllAsync(new string[] { test1, test2 }, new string[] { fixtest1, fixtest2 });
        }
    }
}