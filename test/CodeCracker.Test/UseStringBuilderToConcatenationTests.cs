using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class UseStringBuilderToConcatenationTests : CodeFixTest<UseStringBuilderToConcatenationAnalyzer, UseStringBuilderToConcatenationCodeFixProvider>
    {
        [Fact]
        public void IgnoresConcatenationMinorThanThree()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = ""B"" + ""C"";
            }
        }
    }";

            VerifyCSharpHasNoDiagnostics(test);

        }

        [Fact]
        public void IgnoresVariableDifferentOfString()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = 1 + 1 + 1 + 1;
            }
        }
    }";

            VerifyCSharpHasNoDiagnostics(test);

        }

        [Fact]
        public void CreateDiagnosticsWhenConcatenationGreaterThanTwo()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = ""A"" + ""B"" + ""C"";
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = UseStringBuilderToConcatenationAnalyzer.DiagnosticId,
                Message = "Use 'StringBuilder' instead of concatenation.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            VerifyCSharpDiagnostic(test, expected);

        }

        [Fact]
        public void FixReplacesConcatenationWithoutVariable()
        {
            const string test = @"
    using System;
    using System.Text;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = ""A"" + ""B"" + ""C"";
            }
        }
    }";
            const string expected = @"
    using System;
    using System.Text;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = new StringBuilder().Append(""A"").Append(""B"").Append(""C"").ToString();
            }
        }
    }";

            VerifyCSharpFix(test, expected);

        }

        [Fact]
        public void FixReplacesConcatenationWithVariable()
        {
            const string test = @"
    using System;
    using System.Text;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var b = ""D"";
                var a = ""A"" + ""B"" + ""C"" + b;
            }
        }
    }";
            const string expected = @"
    using System;
    using System.Text;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var b = ""D"";
                var a = new StringBuilder().Append(""A"").Append(""B"").Append(""C"").Append(b).ToString();
            }
        }
    }";

            VerifyCSharpFix(test, expected);

        }
    }
}
