using System;
using System.Threading.Tasks;
using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
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

#pragma warning disable CC0063
        [Fact]
        public async Task IfAbbreviatedUriConstructorFoundAndUriIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new Uri(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(9, 31, () => new Uri("foo")));
        }

        [Fact]
        public async Task IfUriConstructorFoundAndUriIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new System.Uri(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(9, 38, () => new Uri("foo")));
        }

        [Fact]
        public async Task IfUriConstructorWithUriKindFoundAndUriIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new Uri(""http://wrong"", UriKind.Relative)");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(9, 31, () => new Uri("http://wrong", UriKind.Relative)));
        }
#pragma warning restore CC0063

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

        [Fact]
        public async Task IfUriConstructorUsingNullFoundDoNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new System.Uri(null)");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfUriConstructorNotUsingLiteralFoundDoNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"var uri = new System.Uri(new object().ToString())");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfAbbreviatedUriConstructorFoundAndUriIsIncorrectAndItsNotSystemUriDoNotCreatesDiagnostic()
        {
            const string code = @"
namespace ConsoleApplication1
{
    class Uri
    {
        public Uri(string uri) { }

        public void Test() {
            var uri = new Uri(""whoCares"");
        }
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(code);
        }

        private static DiagnosticResult CreateDiagnosticResult(int line, int column, Action getErrorMessageAction)
        {
            return new DiagnosticResult(DiagnosticId.Uri.ToDiagnosticId(), DiagnosticSeverity.Error)
                .WithLocation(line, column)
                .WithMessage(GetErrorMessage(getErrorMessageAction));
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
            return new UriAnalyzer();
        }
    }
}