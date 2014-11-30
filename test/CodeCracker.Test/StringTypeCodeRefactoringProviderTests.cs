using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeRefactorings;
using TestHelper;
using Xunit;

namespace CodeCracker.Test
{
    public class StringTypeCodeRefactoringProviderTests : RefactoringVerifier
    {
        protected override CodeRefactoringProvider GetCSharpCodeRefactoringProvider()
        {
            return new StringTypeCodeRefactoringProvider();
        }

        [Fact]
        public Task DoesNotTriggerOnEmptySource()
        {
            return VerifyNoCSharpRefactoringAsync("├┤");
        }

        [Fact]
        public Task DoesNotTriggerNumericLiteral()
        {
            return VerifyNoCSharpRefactoringAsync(@"
class C
{
    void M()
    {
        var i = ├5┤;
    }
}");
        }

        [Fact]
        public Task ConvertStringToVerbatim()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello ├┤world"";
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

            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToVerbatimId);
        }

        [Fact]
        public Task ConvertStringToVerbatimSelection()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ├""Hello world""┤;
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


            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToVerbatimId);
        }

        [Fact]
        public Task StringToVerbatimHandleQuotes()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello ├┤\""world\"""";
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
                    return VerifyCSharpRefactoringAsync(before, expected,
            StringTypeCodeRefactoringProvider.ToVerbatimId);
        }

        [Fact]
        public Task StringToVerbatimHandleCrLf()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello ├┤\r\nworld"";
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

            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToVerbatimId);
        }

        [Fact]
        public Task StringToVerbatimHandleTab()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello\t├┤world"";
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

            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToVerbatimId);
        }

        [Fact]
        public Task StringToVerbatimHandleBackslash()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ""Hello \\ ├┤world"";
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

            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToVerbatimId);
        }

        [Fact]
        public Task ConvertVerbatimToString()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = @""Hello ├┤world"";
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

            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToRegularId);
        }

        [Fact]
        public Task ConvertVerbatimToStringSelection()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = ├@""Hello world""┤;
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

            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToRegularId);
        }

        [Fact]
        public Task VerbatimToStringHandleCrlf()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = @""Hello ├┤" + "\r\n" + @"world"";
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

            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToRegularId);
        }

        [Fact]
        public Task VerbatimToStringHandleTab()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = @""Hello"+"\t" + @"├┤world"";
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

            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToRegularId);
        }

        [Fact]
        public Task VerbatimToStringHandleBacklash()
        {
            const string before = @"
class C
{
    void M()
    {
        var s = @""Hello \ ├┤world"";
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

            return VerifyCSharpRefactoringAsync(before, expected,
                StringTypeCodeRefactoringProvider.ToRegularId);
        }
    }
}
