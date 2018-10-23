using System.Threading.Tasks;
using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using Xunit;

namespace CodeCracker.Test.CSharp.Refactoring
{
    public class AddBracesToSwitchSectionsTests : CodeFixVerifier<AddBracesToSwitchSectionsAnalyzer, AddBracesToSwitchSectionsCodeFixProvider>
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
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpMethod());
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
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpMethod());
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
            await VerifyCSharpHasNoDiagnosticsAsync(test.WrapInCSharpMethod());
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
            var diagnostic = new DiagnosticResult(DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 13)
                .WithMessage("Add braces for each section in this switch");
            await VerifyCSharpDiagnosticAsync(test.WrapInCSharpMethod(), diagnostic);
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
            var diagnostic = new DiagnosticResult(DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 13)
                .WithMessage("Add braces for each section in this switch");
            await VerifyCSharpDiagnosticAsync(test.WrapInCSharpMethod(), diagnostic);
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
            var diagnostic = new DiagnosticResult(DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(10, 13)
                .WithMessage("Add braces for each section in this switch");
            await VerifyCSharpDiagnosticAsync(test.WrapInCSharpMethod(), diagnostic);
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
            await VerifyCSharpFixAsync(test.WrapInCSharpMethod(), expected.WrapInCSharpMethod());
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
            await VerifyCSharpFixAsync(test.WrapInCSharpMethod(), expected.WrapInCSharpMethod());
        }

        [Fact]
        public async Task FixAddsBracesWhenSomeCasesHaveBraces()
        {
            var test = @"switch(x)
{
    case 0:
    {
        Foo();
        break;
    }
    case 1:
        Bar();
        break;
    default:
        Baz();
        break;
}".WrapInCSharpMethod();
            var expected = @"switch(x)
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
}".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, expected);
        }
    }
}