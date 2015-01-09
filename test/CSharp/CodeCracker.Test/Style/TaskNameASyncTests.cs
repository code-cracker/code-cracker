using CodeCracker.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Style
{
    public class TaskNameAsyncTests : CodeFixTest<TaskNameAsyncAnalyzer, TaskNameAsyncCodeFixProvider>
    {
        [Fact]
        public async Task TaskNameAsyncMethodCorrect()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            Task TestASync() {};
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task TaskNameAsyncMethodWhithoutAsyncName()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            Task Test() {};
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = TaskNameAsyncAnalyzer.DiagnosticId,
                Message = TaskNameAsyncAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 13) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ChangeTaskNameWhithoutAsync()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            Task Test() {};
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            Task TestAsync() {};
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }


        [Fact]
        public async Task ChangeTaskNameWhithoutAsyncAndClassImplementation()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            public methodTest()
            {
                await Test();
            }

            public Task<bool> Test()
            {
                return true;
            }
        }

    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            public methodTest()
            {
                await TestAsync();
            }

            public Task<bool> TestAsync()
            {
                return true;
            }
        }

    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}