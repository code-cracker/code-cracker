using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class NameOfTests : CodeFixTest<NameOfAnalyzer, NameOfCodeFixProvider>
    {
        [Fact]
        public async Task IgnoreIfStringLiteralIsWhiteSpace()
        {
            var test = @"
public class TypeName
{
    void Foo()
    {
        var whatever = """";
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreIfStringLiteralIsNull()
        {
            var test = @"
public class TypeName
{
    void Foo()
    {
        var whatever = null;
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreIfConstructorHasNoParameters()
        {
            var test = @"
public class TypeName()
{
    public TypeName()
    {
        string whatever = ""b"";
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreIfMethodHasNoParameters()
        {
            var test = @"
public class TypeName
{
    void Foo()
    {
        var whatever = ""b"";
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreIfMethodHasParametersUnlikeOfStringLiteral()
        {
            var test = @"
public class TypeName
{
    void Foo(string a)
    {
        var whatever = ""b"";
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsParameterNameReturnAnalyzerCreatesDiagnostic()
        {
            var source = @"
public class TypeName
{
    void Foo(string b)
    {
        string whatever = ""b"";
    }
}";
            var expected = new DiagnosticResult
            {
                Id = NameOfAnalyzer.DiagnosticId,
                Message = "Use 'nameof(b)' instead of specifying the parameter name.",
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 6, 27) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsParameterNameInConstructorFixItToNameof()
        {
            const string source = @"
public class TypeName
{
    public TypeName(string b)
    {
        string whatever = ""b"";
    }
}";

            var fixtest = @"
public class TypeName
{
    public TypeName(string b)
    {
        string whatever = nameof(b);
    }
}";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsParameterNameInConstructorFixItToNameofMustKeepComments()
        {
            const string source = @"
public class TypeName
{
    public TypeName(string b)
    {
        //a
        string whatever = ""b"";//d
        //b
    }
}";

            var fixtest = @"
public class TypeName
{
    public TypeName(string b)
    {
        //a
        string whatever = nameof(b);//d
        //b
    }
}";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsParameterNameInMethodFixItToNameof()
        {
            const string source = @"
public class TypeName
{
    void Foo(string b)
    {
        string whatever = ""b"";
    }
}";

            var fixtest = @"
public class TypeName
{
    void Foo(string b)
    {
        string whatever = nameof(b);
    }
}";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingStringLiteralEqualsParameterNameInMethodMustKeepComments()
        {
            const string source = @"
public class TypeName
{
    void Foo(string b)
    {
        //a
        string whatever = ""b""//d;
        //b
    }
}";

            var fixtest = @"
public class TypeName
{
    void Foo(string b)
    {
        //a
        string whatever = nameof(b)//d;
        //b
    }
}";

            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}