using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class UseEmptyStringTest  : CodeFixVerifier<UseEmptyStringAnalyzer, UseEmptyStringCodeFixProvider>
    {
        [Fact]
        public async Task UsingEmptyStringShouldNotGenerateDiagnosticResult()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var t = """";
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task UsingOtherStringMethodShouldNotGenerateDiagnosticResult()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var t = string.IsNullOrEmpty("""");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task UsingStringDotEmptyInGeneratedCodeShouldNotGenerateDiagnosticResult()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        [GeneratedCode]
        class TypeName
        {
            public void Foo()
            {
                var t = string.IsNullOrEmpty(string.Empty);
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task UsingStringDotEmptyShouldGenerateDiagnosticResult()
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

            var expected = CreateEmptyStringDiagnosticResult(10, 25);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task UsingStringDotEmptyCamelCaseShouldGenerateDiagnosticResult()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = String.Empty;
            }
        }
    }";

            var expected = CreateEmptyStringDiagnosticResult(10, 25);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task UsingStringDotEmptyAsMethodArgumentShouldGenerateDiagnosticResult()
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
                Foo(string.Empty);
            }
        }
    }";

            var expected = CreateEmptyStringDiagnosticResult(12, 21);

            await VerifyCSharpDiagnosticAsync(test, expected);
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
                test(string.Empty);
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
                test("""");
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixAllInDocChangeMethodToStringEmpty()
        {
            var test = "var s = string.Empty; s = s + \"a\"".WrapInCSharpMethod();
            var expected = "var s = \"\"; s = s + \"a\"".WrapInCSharpMethod();
            await VerifyCSharpFixAllAsync(test, expected);
        }

        [Fact]
        public async Task FixAllInSolutionChangeMethodToStringEmpty()
        {
            var test1 = "var s = string.Empty + string.Empty;s = s + \"a\";".WrapInCSharpMethod();
            var test2 = "var s = string.Empty + string.Empty;s = s + \"a\";".WrapInCSharpMethod(typeName: "AnotherType");
            var expected1 = "var s = \"\" + \"\";s = s + \"a\";".WrapInCSharpMethod();
            var expected2 = "var s = \"\" + \"\";s = s + \"a\";".WrapInCSharpMethod(typeName: "AnotherType");
            await VerifyCSharpFixAllAsync(new[] { test1, test2 }, new[] { expected1, expected2 });
        }

        [Fact]
        public async Task TwoEmptyStringsGenerateTwoDiagnostics()
        {
            var test = "var s = string.Empty + string.Empty;".WrapInCSharpMethod();
            var expected1 = CreateEmptyStringDiagnosticResult(10, 21);
            var expected2 = CreateEmptyStringDiagnosticResult(10, 36);

            await VerifyCSharpDiagnosticAsync(test, expected1, expected2);
        }

        [Fact]
        public async Task IgnoreAttribute()
        {
            const string test = @"[assembly: System.Reflection.AssemblyDescription(string.Empty)]";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        private static DiagnosticResult CreateEmptyStringDiagnosticResult(int expectedRow, int expectedColumn)
        {
            return new DiagnosticResult(DiagnosticId.UseEmptyString.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(expectedRow, expectedColumn)
                .WithMessage("Use \"\" instead of 'string.Empty'");
        }
    }
}