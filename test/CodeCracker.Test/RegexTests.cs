using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class RegexTests : CodeFixVerifier
    {
        [Fact]
        public void IfRegexIdentifierIsNotFoundDoesNotCreateDiagnostic()
        {
            var test = @"
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
            var test = @"
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
            var source = @"
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

            var message = "";
            try
            {
                System.Text.RegularExpressions.Regex.Match("", "[");
            }
            catch (ArgumentException e)
            {
                message = e.Message;
            }

            var expected = new DiagnosticResult
            {
                Id = RegexAnalyzer.DiagnosticId,
                Message = message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 64) }
            };

            VerifyCSharpDiagnostic(source, expected);
        }

        [Fact]
        public void IfRegexIdentifierFoundAndAbbreviatedAndRegexTextIsIncorrectCreatesDiagnostic()
        {
            var source = @"
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

            var message = "";
            try
            {
                System.Text.RegularExpressions.Regex.Match("", "[");
            }
            catch (ArgumentException e)
            {
                message = e.Message;
            }

            var expected = new DiagnosticResult
            {
                Id = RegexAnalyzer.DiagnosticId,
                Message = message,
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