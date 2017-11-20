using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class TaskNameAsyncTests : CodeFixVerifier<TaskNameAsyncAnalyzer, TaskNameAsyncCodeFixProvider>
    {
        [Fact]
        public async Task TaskNameAsyncMethodEqualsNameMethodInterface()
        {
            const string source = @"
            using System.Threading.Tasks;

            namespace ConsoleApplication1
            {
                public interface IBar
                {
                    Task Foo();
                }

                public class Bar : IBar
                {
                    public Task Foo()
                    { }
                }
            }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.TaskNameAsync.ToDiagnosticId(),
                Message = string.Format(TaskNameAsyncAnalyzer.MessageFormat, "FooAsync"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 26) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task TaskNameAsyncMethodCorrect()
        {
            const string source = @"
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        public class Foo
        {
            Task TestAsync() {};
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task TaskNameAsyncMethodWhithoutAsyncNameAndOverridesShouldNotCreateDiagnostic()
        {
            const string source = @"
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        public class Foo
        {
            overrides Task Test() {};
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task TaskNameAsyncMethodWhithoutAsyncNameAndShadowsShouldNotCreateDiagnostic()
        {
            const string source = @"
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        public class Foo
        {
            new Task Test() {};
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task TaskNameAsyncMethodWhithoutAsyncName()
        {
            const string source = @"
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        public class Foo
        {
            Task Test() {};
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.TaskNameAsync.ToDiagnosticId(),
                Message = string.Format(TaskNameAsyncAnalyzer.MessageFormat, "TestAsync"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 8, 18) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ChangeTaskNameWhithoutAsync()
        {
            const string source = @"
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        public class Foo
        {
            Task Test() {};
        }
    }";
            const string fixtest = @"
    using System.Threading.Tasks;

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
    using System.Threading.Tasks;

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
    using System.Threading.Tasks;

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

        [Fact]
        public async Task ChangeTaskNameWhithoutAsyncAndInterfaceImplementation()
        {
            const string source = @"
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        public interface IFoo
        {
            public Task Test()
        }

    }";
            const string fixtest = @"
    using System.Threading.Tasks;

    namespace ConsoleApplication1
    {
        public interface IFoo
        {
            public Task TestAsync()
        }

    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task IgnoreMethodFromImplicitlyImplementedInterface()
        {
            const string source = @"
using System.Threading.Tasks;
public interface IBar
{
    Task Foo();
}
public class Bar : IBar
{
    public Task Foo()
    {
        throw new System.NotImplementedException();
    }
}";
            //we still get the diagnostic for the interface itself
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.TaskNameAsync.ToDiagnosticId(),
                Message = string.Format(TaskNameAsyncAnalyzer.MessageFormat, "FooAsync"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 5, 10) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IgnoreMethodFromExplicitlyImplementedInterface()
        {
            const string source = @"
using System.Threading.Tasks;
public interface IBar
{
    Task Foo();
}
public class Bar : IBar
{
    Task IBar.Foo()
    {
        throw new System.NotImplementedException();
    }
}";
            //we still get the diagnostic for the interface itself
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.TaskNameAsync.ToDiagnosticId(),
                Message = string.Format(TaskNameAsyncAnalyzer.MessageFormat, "FooAsync"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 5, 10) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }
    }
}