using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class RegexTests : CodeFixVerifier
    {
        [Fact]
        public void IfRegexIdentifierIsNotFoundDoesNotCreateDiagnostic()
        {
            string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                 Regex.Match("""", ""["");
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void IfRegexIdentifierFoundButRegexTextIsCorrectDoesNotCreateDiagnostic()
        {
            string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                 Regex.Match("""", ""[]"");
            }
        }
    }";
            VerifyCSharpHasNoDiagnostics(test);
        }

        [Fact]
        public void IfRegexIdentifierFoundAndRegexTextIsIncorrectCreatesDiagnostic()
        {
            string source = @"
    using System;
    using System.Text.RegularExpressions;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                System.Text.RegularExpressions.Regex.Match("""", ""["");
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = RegexAnalyzer.DiagnosticId,
                Message = @"parsing ""["" - Unterminated [] set.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 64) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void IfRegexIdentifierFoundAndAbbreviatedAndRegexTextIsIncorrectCreatesDiagnostic()
        {
            string source = @"
    using System;
    using System.Text.RegularExpressions;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                Regex.Match("""", ""["");
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = RegexAnalyzer.DiagnosticId,
                Message = @"parsing ""["" - Unterminated [] set.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 33) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new RegexAnalyzer();
        }
    }
}