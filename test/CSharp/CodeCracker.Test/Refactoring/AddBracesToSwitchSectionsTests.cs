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
            const string test = @"switch(x)
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
            const string test = @"switch(x)
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
            const string test = @"switch(x)
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
            const string test = @"switch(x)
{
    case 0:
        Foo();
        break;
}";
            var diagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId(),
                Message = "Add braces for each section in this switch",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] {new DiagnosticResultLocation("Test0.cs", 10, 17)}
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInMethod(), diagnostic);
        }

        [Fact]
        public async Task CreateDiagnosticWhenNotAllSwitchSectionsHaveBraces()
        {
            const string test = @"switch(x)
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
                Id = DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId(),
                Message = "Add braces for each section in this switch",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInMethod(), diagnostic);
        }

        [Fact]
        public async Task CreateDiagnosticWhenDefaultSectionsHasNoBraces()
        {
            const string test = @"switch(x)
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
                Id = DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId(),
                Message = "Add braces for each section in this switch",
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test.WrapInMethod(), diagnostic);
        }

        [Fact]
        public async Task FixAddsBracesForSingleSection()
        {
            const string test = @"switch(x)
{
    case 0:
        Foo();
        break;
}";
            const string expected = @"switch(x)
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
            const string test = @"switch(x)
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
            const string expected = @"switch(x)
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