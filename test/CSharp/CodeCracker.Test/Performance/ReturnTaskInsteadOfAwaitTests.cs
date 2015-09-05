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
        public async Task IgnoreGenericTasksWhenWorkIsDoneAfterAwait()
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
        public async Task IgnoreMultipeAwaits()
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
        public async Task IgnoreWhenMultipeAwaitsInsideABlock()
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
        public async Task IgnoreSwitchSectionWithoutAwait()
        {
            const string test = @"
                async Task FooAsync(int x)
                {
                    Console.WriteLine(""foo"");
                    switch (x)
                    {
                        case 1:
                            await Task.Delay(10);
                            break;
                        case 2:
                            Console.WriteLine(""foo"");
                            break;
                        default:
                            await Task.Delay(42);
                            break;
                    }
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
        [Fact]
        public async Task IgnoreSwitchWithOutDefault()
        {
            const string test = @"
                async Task FooAsync(int x)
                {
                    Console.WriteLine(""foo"");
                    switch (x)
                    {
                        case 1:
                            await Task.Delay(10);
                            break;
                        case 2:
                            await Task.Delay(42);
                            break;
                    }
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresWhenAwaitIsAfterBlock()
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
                    await Task.Delay(200);
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
        [Fact]
        public async Task IgnoreAwaitsAfterBranching()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(""foo"");
                    if (true)
                       await Task.Delay(200);
                    else
                        Console.WriteLine(""42"");
                    
                    await Task.Delay(200);
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
        [Fact]
        public async Task IgnoreAwaitsOnlyInSingleBranch()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(""foo"");
                    if (true)
                       await Task.Delay(200);
                }
                ";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
        [Fact]
        public async Task IgnoreReturnWithVoid()
        {
            const string test = @"
                async void FooAsync()
                {
                    Console.WriteLine(42);rv 
                    await Task.Delay(200);
                    return;
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
        public async Task ReturnTaskDirectlyWithDeepBranching()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(""foo"");
                    if (true)
                        if (true)
                            await Task.Delay(200);
                        else
                           await Task.Delay(200);
                    else
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
        public async Task ReturnTaskDirectlyWithDeepBranchingAndMoreStatements()
        {
            const string test = @"
                async Task FooAsync()
                {
                    Console.WriteLine(""foo"");
                    if (true)
                        if (true)
                            await Task.Delay(10);
                        else
                           await Task.Delay(20);
                    else
                    {
                        if (true)
                        {
                            Console.WriteLine(""foo"");
                            await Task.Delay(30);
                        }
                        else
                           await Task.Delay(40);
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
        public async Task ReturnTaskDirectlyWithSwitchBranching()
        {
            const string test = @"
                async Task FooAsync(int x)
                {
                    Console.WriteLine(""foo"");
                    switch (x)
                    {
                        case 1:
                            await Task.Delay(10);
                            break;
                        case 2:
                            await Task.Delay(20);
                            break;
                        case 3:
                            await Task.Delay(30);
                            break;
                        default:
                            await Task.Delay(42);
                            break;
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
        public async Task ReturnTaskDirectlyWithBothSwitchIfBranching()
        {
            const string test = @"
                async Task FooAsync(int x)
                {
                    Console.WriteLine(""foo"");                       
                    if (true)
                        await Task.Delay(200);
                    else
                    {
                        switch (x)
                        {
                            case 1:
                                if (true)
                                    await Task.Delay(200);
                                else
                                    await Task.Delay(200);
                                break;
                            case 2:
                                await Task.Delay(20);
                                break;
                            case 3:
                                await Task.Delay(30);
                                break;
                            default:
                                await Task.Delay(42);
                                break;
                        }
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
    }
}