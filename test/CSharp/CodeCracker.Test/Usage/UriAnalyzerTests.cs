using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
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
        public async Task IfUriConstructorFoundAndUriIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new Uri(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(9, 31));
        }

        [Fact]
        public async Task IfUriConstructorWithUriKindFoundAndUriIsCorrectDoNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new Uri(""foo"", UriKind.Relative)");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        private static DiagnosticResult CreateDiagnosticResult(int line, int column)
        {
            return new DiagnosticResult
            {
                Id = UriAnalyzer.DiagnosticId,
                Message = "'Invalid URI: The format of the URI could not be determined.'",
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
