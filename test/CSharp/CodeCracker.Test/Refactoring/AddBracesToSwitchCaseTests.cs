using System.Threading.Tasks;
using CodeCracker.Refactoring;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Refactoring
{
    public class AddBracesToSwitchCaseTests : CodeFixTest<AddBracesToSwitchCaseAnalyzer, AddBracesToSwitchCaseCodeFix>
    {
        [Fact]
        public async Task IgnoresWhenSwitchSectionAlreadyHasBraces()
        {
            string test = @"switch(x)
{
    case 0:
    {
        Foo();
        break;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInMethod());
        }

        [Fact]
        public async Task IgnoresWhenSwitchSectionAlreadyHasBracesFollowedByBreak()
        {
            string test = @"switch(x)
{
    case 0:
        {
            Foo();
        }
        break;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInMethod());
        }

        [Fact]
        public async Task CreateDiagnosticWhenSwitchSectionHasNoBraces()
        {
            string test = @"switch(x)
{
    case 0:
        Foo();
        break;
}";
            var diagnostic = new DiagnosticResult
            {
                Id = AddBracesToSwitchCaseAnalyzer.DiagnosticId,
                Message = "Add braces for this case",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] {new DiagnosticResultLocation("Test0.cs", 12, 5)}
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInMethod(), diagnostic);
        }

        [Fact]
        public async Task CreateDiagnosticWhenSwitchSectionHasNoBracesAndMultipleCases()
        {
            string test = @"switch(x)
{
    case 0:
    case 1:
    case 2:
        Foo();
        break;
}";
            var diagnostic = new DiagnosticResult
            {
                Id = AddBracesToSwitchCaseAnalyzer.DiagnosticId,
                Message = "Add braces for this case",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 5) }
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInMethod(), diagnostic);
        }

        [Fact]
        public async Task FixAddsBraces()
        {
            string test = @"switch(x)
{
    case 0:
        Foo();
        break;
}";
            string expected = @"switch(x)
{
    case 0:
    {
        Foo();
        break;
    }
}";
            await VerifyCSharpFixAsync(test.WrapInMethod(), expected.WrapInMethod());
        }

        [Fact]
        public async Task FixAddsBracesWithMultipleCases()
        {
            string test = @"switch(x)
{
    case 0:
    case 1:
    case 2:
        Foo();
        break;
}";
            string expected = @"switch(x)
{
    case 0:
    case 1:
    case 2:
    {
        Foo();
        break;
    }
}";
            await VerifyCSharpFixAsync(test.WrapInMethod(), expected.WrapInMethod());
        }
    }
}
