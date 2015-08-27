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
        public async Task IgnoresAwaitInsideLoops()
        {
            const string test = @"
                async Task FooAsync()
                {   
                    while (true) 
                    {
                        Console.WriteLine(42);
                        await Task.Delay(200);
                    }
                }
                async Task Foo2Async()
                {   
                    do 
                    {
                        Console.WriteLine(42);
                        await Task.Delay(200);
                    } while (true)
                }
                async Task Foo3Async()
                {   
                    for (;;)
                    {
                        Console.WriteLine(42);
                        await Task.Delay(200);
                    }
                }
                async Task Foo4Async()
                {   
                    foreach (var x in new[] { 1,2,3 })
                    {
                        Console.WriteLine(42);
                        await Task.Delay(200);
                    }
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
        [Fact]
        public async Task IgnoresGenericTasksWhenWorkIsDoneAfterAwait()
        {
            const string test = @"
                async Task<int> FooAsync()
                {
                    Console.WriteLine(""Done Waiting"");
                    await Task.Delay(200);
                    return 42;
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
        [Fact]
        public async Task ReturnTaskDirectlyWithMultipleBranchesWithOutBlocks()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(""foo"");
                    if (true)
                       await Task.Delay(200);
                    else
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
        public async Task MultipleReturnsWithAwaits()
        {
            const string test = @"
                async Task<int> FooAsync()
                {
                    if (true)
                    {
                        Console.WriteLine(""Done Waiting"");
                        return await Sum(1, 1);
                    }
                    Console.WriteLine(""Done Waiting"");
                    return await Sum(1, 1);
                }

                async Task<int> Sum(int x, int y) { await Task.Delay(200); return x + y; }
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
        public async Task ReturnWithAwaits()
        {
            const string test = @"
                async Task<int> FooAsync()
                {
                    Console.WriteLine(""Done Waiting"");
                    return await Sum(1, 1);
                }

                async Task<int> Sum(int x, int y) { await Task.Delay(200); return x + y; }
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
        public async Task VoidReturnWithAwaits()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(42);
                    await Task.Delay(200);
                    return;
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