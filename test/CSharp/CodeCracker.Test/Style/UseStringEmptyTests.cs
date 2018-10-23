using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
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

            var expected = new DiagnosticResult(DiagnosticId.UseStringEmpty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 25)
                .WithMessage("Use 'String.Empty' instead of \"\"");
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task WhenHasStringInParameterShouldNotRaiseDiagnostic()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo(string name = "")
            {
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);

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

            var expected = new DiagnosticResult(DiagnosticId.UseStringEmpty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(12, 21)
                .WithMessage("Use 'String.Empty' instead of \"\"");
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

        [Fact]
        public async Task FixAllInDocChangeMethodToStringEmpty()
        {
            var test = @"var s = """" + """";".WrapInCSharpMethod();
            var expected = @"var s = string.Empty + string.Empty;".WrapInCSharpMethod();
            await VerifyCSharpFixAllAsync(test, expected);
        }

        [Fact]
        public async Task FixAllInSolutionChangeMethodToStringEmpty()
        {
            var test1 = @"var s = """" + """";".WrapInCSharpMethod();
            var test2 = @"var s = """" + """";".WrapInCSharpMethod(typeName: "AnotherType");
            var expected1 = @"var s = string.Empty + string.Empty;".WrapInCSharpMethod();
            var expected2 = @"var s = string.Empty + string.Empty;".WrapInCSharpMethod(typeName: "AnotherType");
            await VerifyCSharpFixAllAsync(new[] { test1, test2 }, new[] { expected1, expected2 });
        }

        [Fact]
        public async Task TwoEmptyStringsGenerateTwoDiagnostics()
        {
            var test = @"var s = """" + """";".WrapInCSharpMethod();
            var expected1 = new DiagnosticResult(DiagnosticId.UseStringEmpty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 21)
                .WithMessage("Use 'String.Empty' instead of \"\"");
            var expected2 = new DiagnosticResult(DiagnosticId.UseStringEmpty.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 26)
                .WithMessage("Use 'String.Empty' instead of \"\"");
            await VerifyCSharpDiagnosticAsync(test, expected1, expected2);
        }

        [Fact]
        public async Task IgnoreAttribute()
        {
            const string test = @"[assembly: System.Reflection.AssemblyDescription("""")]";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
    }
}