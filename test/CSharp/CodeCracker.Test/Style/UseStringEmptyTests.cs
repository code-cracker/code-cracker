using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class UseStringEmptyTests : CodeFixVerifier<UseStringEmptyAnalyzer, UseStringEmptyCodeFixProvider>
    {
        [Fact]
        public async Task UsingStringEmpty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = string.Empty;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task NotUsingStringEmpty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = """";
            }
        }
    }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UseStringEmpty.ToDiagnosticId(),
                Message = "Use 'String.Empty' instead of \"\"",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 25) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }


        [Fact]
        public async Task MethodNotUsingStringEmpty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string x) => x;

            public void test()
            {
                Foo("""");
            }
        }
    }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UseStringEmpty.ToDiagnosticId(),
                Message = "Use 'String.Empty' instead of \"\"",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 21) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task FixChangeToStringEmpty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = """";
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = string.Empty;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixChangeMethodToStringEmpty()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void test(string x) => x;

            public void Foo()
            {
                test("""");
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void test(string x) => x;

            public void Foo()
            {
                test(string.Empty);
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }
    }
}
