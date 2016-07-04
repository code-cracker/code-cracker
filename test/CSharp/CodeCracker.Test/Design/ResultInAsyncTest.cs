using CodeCracker.CSharp.Design;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class ResultInAsyncTest : CodeFixVerifier<ResultInAsyncAnalyzer, ResultInAsyncCodeFixProvider>
    {
        [Fact]
        public async Task ResultInNonAsyncMethodIsOk()
        {
            const string test = @"
                using System.Threading.Tasks;
                public class MyClass
                {
                    public Task Execute()
                    {
                        return Asynchronous().Result;
                    }

                    public async Task Asynchronous()
                    {
                        return;
                    }
                }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WarningIfResultInAsync()
        {
            const string test = @"
                using System.Threading.Tasks;
                public class MyClass
                {
                    public async Task<int> Execute()
                    {
                        return Asynchronous().Result;
                    }

                    public async Task<int> Asynchronous()
                    {
                        return 5;
                    }
                }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ResultInAsync.ToDiagnosticId(),
                Message = string.Format(ResultInAsyncAnalyzer.MessageFormat.ToString(), "Asynchronous"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 32) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task FixResultInAsync()
        {
            const string source = @"
                using System.Threading.Tasks;
                public class MyClass
                {
                    public async Task<int> Execute()
                    {
                        return Asynchronous().Result;
                    }

                    public async Task<int> Asynchronous()
                    {
                        return 5;
                    }
                }";
            const string fixtest = @"
                using System.Threading.Tasks;
                public class MyClass
                {
                    public async Task<int> Execute()
                    {
                        return await Asynchronous();
                    }

                    public async Task<int> Asynchronous()
                    {
                        return 5;
                    }
                }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WarningIfNestedResultInAsync()
        {
            const string test = @"
        namespace Nested.Namespaces
        {
            using System.Threading.Tasks;
            public class ParentClass
            {
                public class MyClass
                {
                    public async Task Execute()
                    {
                        var x = ParentClass.MyClass.Asynchronous().Result.Length;
                    }

                    public static async Task<string> Asynchronous()
                    {
                        return ""Test"";
                    }
                }
            }
        }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.ResultInAsync.ToDiagnosticId(),
                Message = string.Format(ResultInAsyncAnalyzer.MessageFormat.ToString(), "Asynchronous"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 53) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task FixNestedResultInAsync()
        {
            const string source = @"
        namespace Nested.Namespaces
        {
            using System.Threading.Tasks;
            public class ParentClass
            {
                public class MyClass
                {
                    public async Task Execute()
                    {
                        var x = MyClass.Asynchronous().Result.Length;
                    }

                    public static async Task<string> Asynchronous()
                    {
                        return ""Test"";
                    }
                }
            }
        }";
            const string fixtest = @"
        namespace Nested.Namespaces
        {
            using System.Threading.Tasks;
            public class ParentClass
            {
                public class MyClass
                {
                    public async Task Execute()
                    {
                        var x = (await MyClass.Asynchronous()).Length;
                    }

                    public static async Task<string> Asynchronous()
                    {
                        return ""Test"";
                    }
                }
            }
        }";

            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}