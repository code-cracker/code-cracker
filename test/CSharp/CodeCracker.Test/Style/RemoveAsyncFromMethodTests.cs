using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class RemoveAsyncFromMethodTests : CodeFixVerifier<RemoveAsyncFromMethodAnalyzer, RemoveAsyncFromMethodCodeFixProvider>
    {
        [Fact]
        public async Task MethodWithoutAsync()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            void Test() {};
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task MethodAsyncWithoutAsyncKeyword()
        {
            const string source = @"
    using System.Threading.Tasks;
    namespace ConsoleApplication1
    {
        public class Foo
        {
            Task TestAsync() {}
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task MethodNotAsyncWithAsyncTermination()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            void TestAsync() {};
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.RemoveAsyncFromMethod.ToDiagnosticId(),
                Message = string.Format(RemoveAsyncFromMethodAnalyzer.MessageFormat, "TestAsync"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 18) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task MethodNotAsyncChangeName()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            void TestAsync() {};
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        public class Foo
        {
            void Test() {};
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}