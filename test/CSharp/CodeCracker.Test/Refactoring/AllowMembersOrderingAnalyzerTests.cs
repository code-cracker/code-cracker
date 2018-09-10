using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Testing;
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

            var expected = new DiagnosticResult(DiagnosticId.AllowMembersOrdering.ToDiagnosticId(), DiagnosticSeverity.Hidden)
                .WithLocation(2, 14 + typeDeclaration.Length)
                .WithMessage(AllowMembersOrderingAnalyzer.MessageFormat);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }
    }
}