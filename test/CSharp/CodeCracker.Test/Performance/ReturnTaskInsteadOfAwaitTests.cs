using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class ReturnTaskInsteadOfAwaitTests : DiagnosticVerifier<ReturnTaskInsteadOfAwaitAnalyzer>
    {

        [Fact]
        public async Task IgnoresWhenWorkIsDoneAfterTheAwait()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(42);
                    await Task.Delay(200);
                    Console.WriteLine(""Done Waiting"");
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenWorkIsDone()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(42);
                    await Task.Delay(200);
                    Console.WriteLine(""Done Waiting"");
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenMultipeAwaits()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(42);
                    await Task.Delay(200);
                    Console.WriteLine(""Done Waiting"");
                    await Task.Delay(200);
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenMultipeAwaitsInsideABlock()
        {
            const string test = @"
                async Task FooAsync()
                {
                    if (true)
                    {
                        Console.WriteLine(42);
                        await Task.Delay(200);

                        Console.WriteLine(""foo"");
                        await Task.Delay(200);
                    }
                    else
                    {
                        Console.WriteLine(""foo"");
                        await Task.Delay(200);
                    }
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async Task ReturnTaskDirectlyWithSimpleFunction()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(42);
                    await Task.Delay(200);
                }
                ";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReturnTaskInsteadOfAwait.ToDiagnosticId(),
                Message = "This method can directly return a task.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 2, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task ReturnTaskDirectlyWithMultipleBranches()
        {
            const string test = @"
                async Task FooAsync()
                {
                    if (true)
                    {
                        Console.WriteLine(42);
                        await Task.Delay(200);
                    }
                    else
                    {
                        Console.WriteLine(""foo"");
                        await Task.Delay(200);
                    }
                }
                ";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ReturnTaskInsteadOfAwait.ToDiagnosticId(),
                Message = "This method can directly return a task.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 2, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }


    }
}