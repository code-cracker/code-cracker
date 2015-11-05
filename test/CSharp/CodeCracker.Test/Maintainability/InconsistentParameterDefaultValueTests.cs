using System.Threading.Tasks;
using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Xunit;

namespace CodeCracker.Test.CSharp.Maintainability
{
    public class InconsistentParameterDefaultValueTests : CodeFixVerifier<InconsistentParameterDefaultValueAnalyzer, InconsistentParameterDefaultValueCodeFixProvider>
    {
        [Fact]
        public async Task No_Default_Value_Produces_No_Diagnostics()
        {
            const string source = @"
interface IFoo
{
    void Bar(int x = 42);
}

class Foo : IFoo
{
    public void Bar(int x)
    {
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task No_Base_Definition_Produces_No_Diagnostics()
        {
            const string source = @"
class Foo
{
    public void Bar(int x = 42)
    {
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task Default_Value_When_Interface_Definition_Has_None_Produces_A_Diagnostic()
        {
            const string source = @"
interface IFoo
{
    void Bar(int x);
}

class Foo : IFoo
{
    public void Bar(int x = 42)
    {
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.InconsistentParameterDefaultValue.ToDiagnosticId(),
                Message = "The default value '42' of parameter 'x' doesn't match the default value '(none)' from the base definition 'IFoo.Bar(int)'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 21) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task Default_Value_When_Explicitly_Implemented_Interface_Definition_Has_None_Produces_A_Diagnostic()
        {
            const string source = @"
interface IFoo
{
    void Bar(int x);
}

class Foo : IFoo
{
    void IFoo.Bar(int x = 42)
    {
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.InconsistentParameterDefaultValue.ToDiagnosticId(),
                Message = "The default value '42' of parameter 'x' doesn't match the default value '(none)' from the base definition 'IFoo.Bar(int)'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 19) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task Default_Value_When_Base_Class_Definition_Has_None_Produces_A_Diagnostic()
        {
            const string source = @"
abstract class FooBase
{
    public abstract void Bar(int x);
}

class Foo : FooBase
{
    public override void Bar(int x = 42)
    {
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.InconsistentParameterDefaultValue.ToDiagnosticId(),
                Message = "The default value '42' of parameter 'x' doesn't match the default value '(none)' from the base definition 'FooBase.Bar(int)'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 30) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task Default_Value_Different_From_Interface_Definition_Produces_A_Diagnostic()
        {
            const string source = @"
interface IFoo
{
    void Bar(int x = 0);
}

class Foo : IFoo
{
    public void Bar(int x = 42)
    {
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.InconsistentParameterDefaultValue.ToDiagnosticId(),
                Message = "The default value '42' of parameter 'x' doesn't match the default value '0' from the base definition 'IFoo.Bar(int)'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 21) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task Default_Value_Different_From_Explicitly_Implemented_Interface_Definition_Produces_A_Diagnostic()
        {
            const string source = @"
interface IFoo
{
    void Bar(int x = 0);
}

class Foo : IFoo
{
    void IFoo.Bar(int x = 42)
    {
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.InconsistentParameterDefaultValue.ToDiagnosticId(),
                Message = "The default value '42' of parameter 'x' doesn't match the default value '0' from the base definition 'IFoo.Bar(int)'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 19) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task Default_Value_Different_From_Base_Class_Definition_Produces_A_Diagnostic()
        {
            const string source = @"
abstract class FooBase
{
    public abstract void Bar(int x = 0);
}

class Foo : FooBase
{
    public override void Bar(int x = 42)
    {
    }
}
";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.InconsistentParameterDefaultValue.ToDiagnosticId(),
                Message = "The default value '42' of parameter 'x' doesn't match the default value '0' from the base definition 'FooBase.Bar(int)'.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 30) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task UseValueFromBaseDefinition_CodeFix_Is_Applied()
        {
            const string source = @"
interface IFoo
{
    void Bar(int x = 0);
}

class Foo : IFoo
{
    public void Bar(int x = 42)
    {
    }
}
";
            const string newSource = @"
interface IFoo
{
    void Bar(int x = 0);
}

class Foo : IFoo
{
    public void Bar(int x = 0)
    {
    }
}
";

            await VerifyCSharpFixAsync(source, newSource, codeFixIndex: 0);
        }

        [Fact]
        public async Task RemoveDefaultValue_CodeFix_Is_Applied()
        {
            const string source = @"
interface IFoo
{
    void Bar(int x = 0);
}

class Foo : IFoo
{
    public void Bar(int x = 42)
    {
    }
}
";
            const string newSource = @"
interface IFoo
{
    void Bar(int x = 0);
}

class Foo : IFoo
{
    public void Bar(int x)
    {
    }
}
";

            await VerifyCSharpFixAsync(source, newSource, codeFixIndex: 1);
        }
    }
}
