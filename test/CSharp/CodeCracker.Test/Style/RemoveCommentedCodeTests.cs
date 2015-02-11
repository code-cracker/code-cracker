using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.CSharp.Test.Style
{
    public class RemoveCommentedCodeTests : CodeFixTest<RemoveCommentedCodeAnalyzer, RemoveCommentedCodeCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresSingleWordComment()
        {
            var test = @"//comment".WrapInMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoresRegularComments()
        {
            var test = @"// this is a regular comment".WrapInMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task CreateDiagnosticForSingleLineCommentedCode()
        {
            var test = @"// a = 10;".WrapInMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.RemoveCommentedCode.ToDiagnosticId(),
                Message = RemoveCommentedCodeAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task RemovesCommentedCodePreservingRegularComments()
        {
            var test = @"
            // this comment will be preserved
            // var a = ""this comment will be removed"";
            ".WrapInMethod();
            var fixtest = @"
            // this comment will be preserved
            ".WrapInMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task CreateDiagnosticForMultipleLinesCommentedCode()
        {
            var test = @"
            // if (something)
            // {
            //   DoStuff();
            // }".WrapInMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.RemoveCommentedCode.ToDiagnosticId(),
                Message = RemoveCommentedCodeAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 13) }
            };

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
            ".WrapInMethod();
            var fixtest = @"
            // this comment will be preserved
            ".WrapInMethod();
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
            ".WrapInMethod();
            var fixtest = @"
            // this comment will be preserved
            class Foo
            {
            }
            ".WrapInMethod();
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
            ".WrapInMethod();

            var fixtest = @"
            // this comment will be preserved
            if (a > 3)
            {
            }
            ".WrapInMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }
    }
}