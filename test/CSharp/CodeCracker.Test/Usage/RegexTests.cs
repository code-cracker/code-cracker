using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using System;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class RegexTests : CodeFixVerifier
    {
        [Fact]
        public async Task IfRegexIdentifierIsNotFoundDoesNotCreateDiagnostic()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                 Regex.Match("""", ""["");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfRegexIdentifierFoundButRegexTextIsCorrectDoesNotCreateDiagnostic()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                 Regex.Match("""", ""[]"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfRegexIdentifierFoundAndRegexTextIsIncorrectCreatesDiagnostic()
        {
            const string source = @"
    using System;
    using System.Text.RegularExpressions;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                System.Text.RegularExpressions.Regex.Match("""", ""["");
            }
        }
    }";

            var message = "";
            try
            {
#pragma warning disable CC0010
                System.Text.RegularExpressions.Regex.Match("", "[");
            }
            catch (ArgumentException e)
            {
                message = e.Message;
            }

            var expected = new DiagnosticResult(DiagnosticId.Regex.ToDiagnosticId(), DiagnosticSeverity.Error)
                .WithLocation(11, 64)
                .WithMessage(message);

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IfRegexIdentifierFoundAndAbbreviatedAndRegexTextIsIncorrectCreatesDiagnostic()
        {
            const string source = @"
    using System;
    using System.Text.RegularExpressions;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                Regex.Match("""", ""["");
            }
        }
    }";

            var message = "";
            try
            {
                System.Text.RegularExpressions.Regex.Match("", "[");
#pragma warning restore CC0010
            }
            catch (ArgumentException e)
            {
                message = e.Message;
            }

            var expected = new DiagnosticResult(DiagnosticId.Regex.ToDiagnosticId(), DiagnosticSeverity.Error)
                .WithLocation(11, 33)
                .WithMessage(message);

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() => new RegexAnalyzer();
    }
}
