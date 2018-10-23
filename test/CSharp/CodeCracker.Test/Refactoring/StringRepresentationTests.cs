using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class StringRepresentationTests
        : CodeFixVerifier<StringRepresentationAnalyzer, StringRepresentationCodeFixProvider>
    {

        [Fact]
        public Task DoesNotTriggerOnEmptySource()
        {
            return VerifyCSharpHasNoDiagnosticsAsync("");
        }

        [Fact]
        public Task DoesNotTriggerNumericLiteral()
        {
            return VerifyCSharpHasNoDiagnosticsAsync(@"
class C
{
    void M()
    {
        var i = 5;
    }
}");
        }

        [Fact]
        public Task DoesNotTriggerInInterpolatedString()
        {
            return VerifyCSharpHasNoDiagnosticsAsync(@"
class C
{
    void M()
    {
        const int i = 5;
        var s = $""Hello {i} world"";
    }
}");
        }

        [Fact]
        public Task RegularStringProduceDiagnostic()
        {
            const string source = @"
class C
{
    void M()
    {
        var s = ""Hello world"";
    }
}";

            var expected = new DiagnosticResult(DiagnosticId.StringRepresentation_RegularString.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(6, 17)
                .WithMessage("Change to regular string");
            return VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public Task VerbatimStringProduceDiagnostic()
        {
            const string source = @"
class C
{
    void M()
    {
        var s = @""Hello world"";
    }
}";

            var expected = new DiagnosticResult(DiagnosticId.StringRepresentation_VerbatimString.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(6, 17)
                .WithMessage("Change to verbatim string");
            return VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public Task ConvertStringToVerbatim()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello world"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = @""Hello world"";
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task ConvertStringToVerbatimInMethod()
        {
            const string before = @"
class C
{
    void Foo(string s) => s;
    void M()
    {
        var s = Foo(""Hello world"");
    }
}";

            const string expected = @"
class C
{
    void Foo(string s) => s;
    void M()
    {
        var s = Foo(@""Hello world"");
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task ConvertStringToVerbatimKeepComments()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = /*Hi*/""Hello world"" // Hello message;
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = /*Hi*/@""Hello world"" // Hello message;
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task ConvertStringToVerbatimSelection()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello world"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = @""Hello world"";
    }
}";


            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task StringToVerbatimHandleQuotes()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello \""world\"""";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = @""Hello """"world"""""";
    }
}";
            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        [SuppressMessage(nameof(Style), "CC0065", Justification = "Necessary for test")]
        public Task StringToVerbatimHandleCrLf()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello \r\nworld"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = @""Hello 
world"";
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task StringToVerbatimHandleTab()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello\tworld"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = @""Hello" + "\t" + @"world"";
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task StringToVerbatimHandleBackslash()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello \\ world"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = @""Hello \ world"";
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task ConvertVerbatimToString()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = @""Hello world"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = ""Hello world"";
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task ConvertVerbatimToStringKeepComments()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = /*Hi*/@""Hello world"" // Hello ;
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = /*Hi*/""Hello world"" // Hello ;
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task ConvertVerbatimToStringSelection()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = @""Hello world"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = ""Hello world"";
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task VerbatimToStringHandleCrlf()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = @""Hello " + "\r\n" + @"world"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = ""Hello \r\nworld"";
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task VerbatimToStringHandleTab()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = @""Hello"+"\t" + @"world"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = ""Hello\tworld"";
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }

        [Fact]
        public Task VerbatimToStringHandleBacklash()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = @""Hello \ world"";
    }
}";

            const string expected = @"
class C
{
    void M()
    {
        var s = ""Hello \\ world"";
    }
}";

            return VerifyCSharpFixAsync(before, expected);
        }
    }
}