using System;
using System.Net;
using System.Threading.Tasks;
using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage {
    public class VirtualMethodOnConstructorTests : DiagnosticVerifier {

        [Fact]
        public async Task IfVirtualMethodFoundInConstructorCreatesDiagnostic() {
            const string test = @"
public class Person
{{
	public Person(string foo) 
	{{
		DoFoo(foo);
	}}

	public virtual void DoFoo(string foo) 
	{{ 
	}}
}}";
            var expected = new DiagnosticResult {
                Id = DiagnosticId.VirtualMethodOnConstructor.ToDiagnosticId(),
                Message = VirtualMethodOnConstructorAnalyzer.Message,
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 4, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
            return new VirtualMethodOnConstructorAnalyzer();
        }
    }
}