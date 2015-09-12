using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class ReturnTaskInsteadOfAwaitTests : CodeFixVerifier<ReturnTaskInsteadOfAwaitAnalyzer, ReturnTaskInsteadOfAwaitCodeFixProvider>
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
                Message = "FooAsync can directly return a task.",
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
                Message = "FooAsync can directly return a task.",
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
                Message = "FooAsync can directly return a task.",
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
                Message = "FooAsync can directly return a task.",
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
                Message = "FooAsync can directly return a task.",
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
                Message = "FooAsync can directly return a task.",
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
                Message = "FooAsync can directly return a task.",
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
                Message = "FooAsync can directly return a task.",
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
                Message = "FooAsync can directly return a task.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 2, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task ReturnWithAwaitsFix()
        {
            const string test = @"
                async Task<int> FooAsync()
                {
                    Console.WriteLine(""Done Waiting"");
                    return await Sum(1, 1);
                }

                async Task<int> Sum(int x, int y) { await Task.Delay(200); return x + y; }
                ";
            const string expected = @"
                Task<int> FooAsync()
                {
                    Console.WriteLine(""Done Waiting"");
                    return Sum(1, 1);
                }

                async Task<int> Sum(int x, int y) { await Task.Delay(200); return x + y; }
                ";

            await VerifyCSharpFixAsync(WrapInClass(test), WrapInClass(expected));
        }

        [Fact]
        public async Task SimpleFunctionFix()
        {
            const string test = @"
                private async Task FooAsync()
                {
                    Console.WriteLine(42);
                    await Task.Delay(200);
                }
                ";
            const string expected = @"
                private Task FooAsync()
                {
                    Console.WriteLine(42);
                    return Task.Delay(200);
                }
                ";
            await VerifyCSharpFixAsync(WrapInClass(test), WrapInClass(expected));
        }

        [Fact]
        public async Task MethodWithCommentInfrontOfAsyncFix()
        {
            const string test = @"
                /* foo */ async Task FooAsync()
                {
                    Console.WriteLine(42);
                    await Task.Delay(200);
                }
                ";
            const string expected = @"
                /* foo */ Task FooAsync()
                {
                    Console.WriteLine(42);
                    return Task.Delay(200);
                }
                ";
            await VerifyCSharpFixAsync(WrapInClass(test), WrapInClass(expected));
        }
        [Fact]
        public async Task MethodWithCommentBehindeOfAsyncFix()
        {
            const string test = @"
                async /* foo */ private Task FooAsync()
                {
                    Console.WriteLine(42);
                    await Task.Delay(200);
                }
                ";
            const string expected = @"
                /* foo */ private Task FooAsync()
                {
                    Console.WriteLine(42);
                    return Task.Delay(200);
                }
                ";
            await VerifyCSharpFixAsync(WrapInClass(test), WrapInClass(expected));
        }
        [Fact]
        public async Task AsyncWithVoidReturnFix()
        {
            const string test = @"
                // Important Comment
                async void FooAsync()
                {
                    Console.WriteLine(42);
                    await Task.Delay(200);
                }
                ";
            const string expected = @"
                // Important Comment
                Task FooAsync()
                {
                    Console.WriteLine(42);
                    return Task.Delay(200);
                }
                ";
            await VerifyCSharpFixAsync(WrapInClass(test), WrapInClass(expected));
        }

        [Fact]
        public async Task ReturnTaskDirectlyWithBothSwitchIfBranchingFix()
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

            const string expected = @"
                Task FooAsync(int x)
                {
                    Console.WriteLine(""foo"");                       
                    if (true)
                        return Task.Delay(200);
                    else
                    {
                        switch (x)
                        {
                            case 1:
                                if (true)
                                    return Task.Delay(200);
                                else
                                    return Task.Delay(200);
                                break;
                            case 2:
                                return Task.Delay(20);
                                break;
                            case 3:
                                return Task.Delay(30);
                                break;
                            default:
                                return Task.Delay(42);
                                break;
                        }
                    }
                }
                ";
            await VerifyCSharpFixAsync(WrapInClass(test), WrapInClass(expected));
        }

        private static string WrapInClass(string code)
           => code.WrapInCSharpClass(usings: "using System.Threading.Tasks;");
    }
}