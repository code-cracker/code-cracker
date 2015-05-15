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
                var a = String.Empty;
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
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
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
                var a = String.Empty;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }
    }
}