using System.Threading.Tasks;
using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage {
    public class VirtualMethodOnConstructorTests : DiagnosticVerifier {

        [Fact]
        public async Task IfVirtualMethodFoundInConstructorCreatesDiagnostic() {
            const string test = @"
public class Person
{
	public Person(string foo)
	{
		DoFoo(foo);
	}

	public virtual void DoFoo(string foo)
	{
	}
}";
            var expected = new DiagnosticResult(DiagnosticId.VirtualMethodOnConstructor.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(6, 3)
                .WithMessage(VirtualMethodOnConstructorAnalyzer.Message);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task IfVirtualMethodWithThisFoundInConstructorCreatesDiagnostic() {
            const string test = @"
public class Person
{
	public Person(string foo)
	{
		this.DoFoo(foo);
	}

	public virtual void DoFoo(string foo)
	{
	}
}";
            var expected = new DiagnosticResult(DiagnosticId.VirtualMethodOnConstructor.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(6, 3)
                .WithMessage(VirtualMethodOnConstructorAnalyzer.Message);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }


        [Fact]
        public async Task IfVirtualMethodFoundFromOtherClassInConstructorDoNotCreateDiagnostic() {
            const string test = @"
public class Book
{
	public virtual void DoFoo(string foo)
	{
	}
}
public class Person
{
	public Person(string foo)
	{
		var b = new Book();
        b.DoFoo(foo);
	}
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfVirtualMethodNotFoundInConstructorDoNotCreateDiagnostic() {
            const string test = @"
public class Person
{
	public Person(string foo)
	{
		DoFoo(foo);
	}
	public void DoFoo(string foo)
	{
	}
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IfManyVirtualMethodFoundInConstructorCreatesDiagnostics() {
            const string test = @"
public class Person
{
	public Person(string foo)
	{
		DoFoo(foo);
		DoFoo2(foo);
	}

	public virtual void DoFoo(string foo)
	{
	}
    public virtual void DoFoo2(string foo)
	{
	}
}";
            var expected = new DiagnosticResult(DiagnosticId.VirtualMethodOnConstructor.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(6, 3)
                .WithMessage(VirtualMethodOnConstructorAnalyzer.Message);
            var expected2 = new DiagnosticResult(DiagnosticId.VirtualMethodOnConstructor.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(7, 3)
                .WithMessage(VirtualMethodOnConstructorAnalyzer.Message);
            await VerifyCSharpDiagnosticAsync(test, expected, expected2);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() {
            return new VirtualMethodOnConstructorAnalyzer();
        }

        [Fact]
        public async Task IfNameOfFoundInConstructorDoesNotCreateDiagnostic() {
            const string test = @"
public class Person
{
	public Person(string name)
	{
        throw new System.ArgumentOutOfRangeException(nameof(name), """");
	}
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreParameters() {
            const string test = @"
using System;
class Foo
{
    public Foo(Func<string> bar)
    {
        bar();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
    }
}