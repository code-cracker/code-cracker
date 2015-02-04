using System.Threading.Tasks;
using CodeCracker.Refactoring;
using Microsoft.CodeAnalysis;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Refactoring
{
    public class AddBracesToSwitchSectionsTests : CodeFixTest<AddBracesToSwitchSectionsAnalyzer, AddBracesToSwitchSectionsCodeFix>
    {
        [Fact]
        public async Task IgnoresWhenSingleSwitchSectionAlreadyHasBraces()
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
        public async Task IgnoresWhenSingleSwitchSectionAlreadyHasBracesFollowedByBreak()
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
        public async Task IgnoresWhenAllSwitchSectionsAlreadyHaveBraces()
        {
            string test = @"switch(x)
{
    case 0:
    {
        Foo();
        break;
    }
    case 1:
        {
            Bar();
        }
        break;
    default:
    {
        Baz();
        break;
    }

}";
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInMethod());
        }

        [Fact]
        public async Task CreateDiagnosticWhenSingleSwitchSectionHasNoBraces()
        {
            string test = @"switch(x)
{
    case 0:
        Foo();
        break;
}";
            var diagnostic = new DiagnosticResult
            {
                Id = AddBracesToSwitchSectionsAnalyzer.DiagnosticId,
                Message = "Add braces for each case in this switch",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] {new DiagnosticResultLocation("Test0.cs", 10, 17)}
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInMethod(), diagnostic);
        }

        [Fact]
        public async Task CreateDiagnosticWhenNotAllSwitchSectionsHaveBraces()
        {
            string test = @"switch(x)
{
    case 0:
    {
        Foo();
        break;
    }
    case 1:
        Brz();
        break;
    default:
    {
        Baz();
        break;
    }
}";
            var diagnostic = new DiagnosticResult
            {
                Id = AddBracesToSwitchSectionsAnalyzer.DiagnosticId,
                Message = "Add braces for each case in this switch",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInMethod(), diagnostic);
        }

        [Fact]
        public async Task CreateDiagnosticWhenDefaultSectionsHasNoBraces()
        {
            string test = @"switch(x)
{
    case 0:
    {
        Foo();
        break;
    }
    case 1:
    {
        Bar();
        break;
    }
    default:
        Baz();
        break;
}";
            var diagnostic = new DiagnosticResult
            {
                Id = AddBracesToSwitchSectionsAnalyzer.DiagnosticId,
                Message = "Add braces for each case in this switch",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInMethod(), diagnostic);
        }

        [Fact]
        public async Task FixAddsBracesForSingleSection()
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
        Foo();
        break;
    case 1:
        Bar();
        break;
    default:
        Baz();
        break;
}";
            string expected = @"switch(x)
{
    case 0:
    {
        Foo();
        break;
    }
    case 1:
    {
        Bar();
        break;
    }
    default:
    {
        Baz();
        break;
    }
}";
            await VerifyCSharpFixAsync(test.WrapInMethod(), expected.WrapInMethod());
        }
    }
}
