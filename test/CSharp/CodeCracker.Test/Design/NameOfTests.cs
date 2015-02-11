using CodeCracker.CSharp.Design;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class NameOfTests : CodeFixVerifier<NameOfAnalyzer, NameOfCodeFixProvider>
    {
        [Fact]
        public async Task IgnoreIfStringLiteralIsWhiteSpace()
        {
            const string test = @"
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
            const string test = @"
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
            const string test = @"
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
            const string test = @"
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
            const string test = @"
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
            const string source = @"
public class TypeName
{
    void Foo(string b)
    {
        string whatever = ""b"";
    }
}";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.NameOf.ToDiagnosticId(),
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

            const string fixtest = @"
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

            const string fixtest = @"
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

            const string fixtest = @"
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
        public async Task WhenUsingStringLiteralEqualsSecondParameterNameInMethodFixItToNameof()
        {
            const string source = @"
public class TypeName
{
    void Foo(string a, string b)
    {
        string whatever = ""b"";
    }
}";

            const string fixtest = @"
public class TypeName
{
    void Foo(string a, string b)
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

            const string fixtest = @"
public class TypeName
{
    void Foo(string b)
    {
        //a
        string whatever = nameof(b)//d;
        //b
    }
}";

            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixAll()
        {
            const string source = @"
public class TypeName
{
    void Foo(string a, string b)
    {
        var whatever = ""a"";
        var whatever2 = ""b"";
    }
}";

            const string fixtest = @"
public class TypeName
{
    void Foo(string a, string b)
    {
        var whatever = nameof(a);
        var whatever2 = nameof(b);
    }
}";

            await VerifyFixAllAsync(source, fixtest);
        }

        [Fact]
        public async Task IgnoreAttributes()
        {
            const string test = @"
public class TypeName
{
    [Whatever(""a"")]
    void Foo(string a)
    {
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }
    }
}