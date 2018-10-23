using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class RemoveCommentedCodeTests : CodeFixVerifier<RemoveCommentedCodeAnalyzer, RemoveCommentedCodeCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresSingleWordComment()
        {
            var test = @"//comment".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresRegularComments()
        {
            var test = @"// this is a regular comment".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task CreateDiagnosticForSingleLineCommentedCode()
        {
            var test = @"// a = 10;".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.RemoveCommentedCode.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 13)
                .WithMessage(RemoveCommentedCodeAnalyzer.MessageFormat);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task RemovesCommentedCodePreservingRegularComments()
        {
            var test = @"
            // this comment will be preserved
            // var a = ""this comment will be removed"";
            ".WrapInCSharpMethod();
            var fixtest = @"
            // this comment will be preserved
            ".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task CreateDiagnosticForMultipleLinesCommentedCode()
        {
            var test = @"
            // if (something)
            // {
            //   DoStuff();
            // }".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.RemoveCommentedCode.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(11, 13)
                .WithMessage(RemoveCommentedCodeAnalyzer.MessageFormat);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task RemovesCommentedMultilineCodePreservingRegularComments()
        {
            var test = @"
            // this comment will be preserved
            // if (something)
            // {
            //   DoStuff();
            // }
            ".WrapInCSharpMethod();
            var fixtest = @"
            // this comment will be preserved
            ".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task RemovesNonPerfectClassCommentedCode()
        {
            var test = @"
            // this comment will be preserved
            // class Fee
            class Foo
            {
            }
            ".WrapInCSharpMethod();
            var fixtest = @"
            // this comment will be preserved
            class Foo
            {
            }
            ".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task RemovesNonPerfectIfCommentedCode()
        {
            var test = @"
            // this comment will be preserved
            // if (a > 2)
            if (a > 3)
            {
            }
            ".WrapInCSharpMethod();

            var fixtest = @"
            // this comment will be preserved
            if (a > 3)
            {
            }
            ".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }
    }
}