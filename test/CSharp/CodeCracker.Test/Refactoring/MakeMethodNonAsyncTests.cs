using System.Threading.Tasks;
using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class MakeMethodNonAsyncTests : CodeFixVerifier
    {
        [Fact]
        public async Task ShouldRemoveAsyncKeywordAndReplaceReturnedValuesWithTaskFromResultAsync()
        {
            const string codeFileTemplate = @"using System.Threading.Tasks;
class Test
{{
{0}
}}";

            const string testMethod = @"
public static async Task<int> FooAsync()
{
    return 42;
}";
            var testCode = string.Format(codeFileTemplate, testMethod);
            const string fixedMethod = @"
public static Task<int> FooAsync()
{
    return Task.FromResult(42);
}";
            var fixedCode = string.Format(codeFileTemplate, fixedMethod);

            await VerifyCSharpFixAsync(testCode, fixedCode).ConfigureAwait(false);
        }

        protected override CodeFixProvider GetCodeFixProvider() => new MakeMethodNonAsyncCodeFixProvider();
    }
}
