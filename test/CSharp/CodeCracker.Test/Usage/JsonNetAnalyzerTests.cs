using System.Threading.Tasks;
using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using TestHelper;
using Xunit;

namespace CodeCracker.CSharp.Test.Usage
{
    public class JsonNetTests : CodeFixVerifier
    {
        private const string TestCode = @"
using System;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
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
        public async Task IfDeserializeObjectIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"Newtonsoft.Json.JsonConvert.DeserializeObject<Person>(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(11, 67));
        }

        [Fact]
        public async Task IfAbbreviatedDeserializeObjectIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"JsonConvert.DeserializeObject<Person>(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(11,51));
        }

        [Fact]
        public async Task IfDeserializeObjectIdentifierFoundAndJsonTextIsCorrectDoesNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"Newtonsoft.Json.JsonConvert.DeserializeObject<Person>(""{""name"":""foo""}"")");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfAbbreviateDeserializeObjectIdentifierFoundAndJsonTextIsCorrectDoesNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"JsonConvert.DeserializeObject<Person>(""{""name"":""foo""}"")");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfJObjectParseIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"Newtonsoft.Json.Linq.JObject.Parse(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(11, 48));
        }

        [Fact]
        public async Task IfAbbreviatedJObjectParseIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"JObject.Parse(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(11, 27));
        }

        [Fact]
        public async Task IfJObjectParseIdentifierFoundAndJsonTextIsCorrectDoesNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"JObject.Parse(""{""name"":""foo""}"")");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
        //
        [Fact]
        public async Task IfJArrayParseIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"Newtonsoft.Json.Linq.JArray.Parse(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(11, 47));
        }

        [Fact]
        public async Task IfAbbreviatedJArrayParseIdentifierFoundAndJsonTextIsIncorrectCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"JArray.Parse(""foo"")");
            await VerifyCSharpDiagnosticAsync(test, CreateDiagnosticResult(11, 26));
        }

        [Fact]
        public async Task IfJArrayParseIdentifierFoundAndJsonTextIsCorrectDoesNotCreatesDiagnostic()
        {
            var test = string.Format(TestCode, @"JArray.Parse(""{""name"":""foo""}"")");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        private static DiagnosticResult CreateDiagnosticResult(int line, int column) {
            return new DiagnosticResult {
                Id = DiagnosticId.JsonNet.ToDiagnosticId(),
                Message = "Error parsing boolean value. Path '', line 0, position 0.",
                Severity = DiagnosticSeverity.Error,
                Locations = new[] {new DiagnosticResultLocation("Test0.cs", line, column)}
            };
        }


        protected override DiagnosticAnalyzer GetCSharpDiagnosticAnalyzer()
        {
            return new JsonNetAnalyzer();
        }
    }
}