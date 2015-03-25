using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis.CodeFixes;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class RemoveUnusedVariablesTest : CodeFixVerifier<RemoveUnusedVariablesAnalyzer, RemoveUnusedVariablesCodeFixProvider>
    {
        [Fact]
        public async Task WhenVariableUsedWithinOfMethodDoesNotShouldCreateDiagnostic()
        {
            const string source = @"
    class TypeName
    {
        public int  Foo()
        {
            int a = 10;
            return a;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenVariableUsedWithinOfConstructorDoesNotShouldCreateDiagnostic()
        {
            const string source = @"
class Name
{
	public Name()
	{
		var number = 2;

		var result = number * 3;
        
        return result;
	}
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhenVariableWithinOfMethodDoesNotUsedShouldCreateDiagnostics()
        {
            const string source = @"class Name{public void NewFoo(){int number = 2;}}";
            const string fixtest = @"class Name{public void NewFoo(){}}";

            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenVariableWithinOfConstructorDoesNotUsedShouldCreateDiagnostics()
        {
            const string source = @"class Name{public Name(){var foo = 2;}}";
            const string fixtest = @"class Name{public Name(){}}";

            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}
