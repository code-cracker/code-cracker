using System;
using System.Net;
using System.Threading.Tasks;
using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class IPAddressAnalyzerTests : CodeFixVerifier
    {
        private const string TestCode = @"
using System;
using System.Net;
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

#pragma warning disable CC0064
        [Fact]
        public async Task IfParseIdentifierFoundAndIPAddressTextIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"System.Net.IPAddress.Parse(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(10, 40, () => IPAddress.Parse("foo")));
        }

        [Fact]
        public async Task IfAbbreviatedParseIdentifierFoundAndIPAddressTextIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"IPAddress.Parse(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(10, 29, () => IPAddress.Parse("foo")));
        }
#pragma warning restore CC0064

        [Fact]
        public async Task IfParseIdentifierFoundAndIPAddressTextIsCorrectDoesNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"System.Net.IPAddress.Parse(""127.0.0.1"")");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfAbbreviateParseIdentifierFoundAndIPAddressTextIsCorrectDoesNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"IPAddress.Parse(""127.0.0.1"")");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        private static DiagnosticResult CreateDiagnosticResult(int line, int column, Action getErrorMessageAction) {
            return new DiagnosticResult {
                Id = DiagnosticId.IPAddress.ToDiagnosticId(),
                Message = GetErrorMessage(getErrorMessageAction),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {new DiagnosticResultLocation("Test0.cs", line, column)}
            };
        }

        private static string GetErrorMessage(Action action)
        {
            try
            {
                action.Invoke();
            }
            catch (Exception ex)
            {
                return ex.Message;
            }
            return "";
        }


        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
        {
            return new IPAddressAnalyzer();
        }
    }
}