using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Xunit;
namespace CodeCracker.Test.CSharp.Refactoring
{
    public class AllowMembersOrderingAnalyzerTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer()
        {
            return new AllowMembersOrderingAnalyzer();
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public async void AllowMembersOrderingForEmptyTypeShouldNotTriggerDiagnostic(string typeDeclaration)
        {
            var test = @"
            " + typeDeclaration + @" Foo
            {
            }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public async void AllowMembersOrderingForOneMemberShouldNotTriggerDiagnostic(string typeDeclaration)
        {
            var test = @"
            " + typeDeclaration + @" Foo
            {
                int bar() { return 0; }
            }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public async void AllowMembersOrderingForMoreThanOneMemberShouldTriggerDiagnostic(string typeDeclaration)
        {
            var test = @"
            " + typeDeclaration + @" Foo
            {
                int bar() { return 0; }
                void car() { }
            }";

            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.AllowMembersOrdering.ToDiagnosticId(),
                Message = AllowMembersOrderingAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 2, 14 + typeDeclaration.Length) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }
    }
}