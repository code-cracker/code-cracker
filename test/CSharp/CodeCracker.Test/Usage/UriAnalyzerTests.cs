using System.Threading.Tasks;
using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Usage
{
    public class UriAnalyzerTests : CodeFixVerifier
    {
        private const string TestCode = @"
using System;
namespace ConsoleApplication1
{{
    class Person
    {{
        public Person()
        {{
            {0}
        }}
    }}
}}";

        [Fact]
        public async Task IfAbbreviatedUriConstructorFoundAndUriIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new Uri(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(9, 31));
        }

        [Fact]
        public async Task IfUriConstructorFoundAndUriIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new System.Uri(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(9, 38));
        }

        [Fact]
        public async Task IfUriConstructorWithUriKindFoundAndUriIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new Uri(""http://wrong"", UriKind.Relative)");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(9, 31, "'A relative URI cannot be created because the 'uriString' parameter represents an absolute URI.'"));
        }

        [Fact]
        public async Task IfAbbreviatedUriConstructorWithUriKindFoundAndUriIsCorrectDoNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new Uri(""foo"", UriKind.Relative)");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfUriConstructorWithUriKindFoundAndUriIsCorrectDoNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new System.Uri(""foo"", UriKind.Relative)");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        private static DiagnosticResult CreateDiagnosticResult(int line, int column, string message = "'Invalid URI: The format of the URI could not be determined.'")
        {
            return new DiagnosticResult
            {
                Id = UriAnalyzer.DiagnosticId,
                Message = message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, column) }
            };
        }

        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new UriAnalyzer();
        }
    }
}
