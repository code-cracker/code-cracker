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
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 3) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task IfVirtualMethodWithThisFoundInConstructorCreatesDiagnostic() {
            const string test = @"
public class Person
{{
	public Person(string foo) 
	{{
		this.DoFoo(foo);
	}}

	public virtual void DoFoo(string foo) 
	{{ 
	}}
}}";
            var expected = new DiagnosticResult {
                Id = DiagnosticId.VirtualMethodOnConstructor.ToDiagnosticId(),
                Message = VirtualMethodOnConstructorAnalyzer.Message,
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 3) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }


        [Fact]
        public async Task IfVirtualMethodFoundFromOtherClassInConstructorDoNotCreateDiagnostic() {
            const string test = @"
public class Book
{{
	public virtual void DoFoo(string foo) 
	{{ 
	}}
}}
public class Person
{{
	public Person(string foo) 
	{{
		var b = new Book();
        b.DoFoo(foo);
	}}
}}
";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfVirtualMethodNotFoundInConstructorDoNotCreateDiagnostic() {
            const string test = @"
public class Person
{{
	public Person(string foo) 
	{{
		DoFoo(foo);
	}}

	public void DoFoo(string foo) 
	{{ 
	}}
}}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfManyVirtualMethodFoundInConstructorCreatesDiagnostics() {
            const string test = @"
public class Person
{{
	public Person(string foo) 
	{{
		DoFoo(foo);
		DoFoo2(foo);
	}}

	public virtual void DoFoo(string foo) 
	{{ 
	}}
    public virtual void DoFoo2(string foo) 
	{{ 
	}}
}}";
            var expected = new DiagnosticResult {
                Id = DiagnosticId.VirtualMethodOnConstructor.ToDiagnosticId(),
                Message = VirtualMethodOnConstructorAnalyzer.Message,
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 3) }
            };
            var expected2 = new DiagnosticResult {
                Id = DiagnosticId.VirtualMethodOnConstructor.ToDiagnosticId(),
                Message = VirtualMethodOnConstructorAnalyzer.Message,
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 7, 3) }
            };


            await VerifyCSharpDiagnosticAsync(test, expected, expected2);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
            return new VirtualMethodOnConstructorAnalyzer();
        }
    }
}