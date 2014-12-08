using CodeCracker.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Style
{
    public class RemoveCommentedCodeTests : CodeFixTest<RemoveCommentedCodeAnalyzer, RemoveCommentedCodeCodeFixProvider>
    {

        [Fact]
        public async Task IgnoresRegularComments()
        {
            var test = _(@"// this is a regular comment");
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task CreateDiagnosticForSingleLineCommentedCode()
        {
            var test = _(@"// a = 10;");
            var expected = new DiagnosticResult
            {
                Id = RemoveCommentedCodeAnalyzer.DiagnosticId,
                Message = "If code is commented, it should be removed.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task RemovesCommentedCodePreservingRegularComments()
        {
            var test = _(@"
            // this comment will be preserved
            // var a = ""this comment will be removed"";
            "
            );

            var fixtest = _(@"
            // this comment will be preserved
            "
            );
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task CreateDiagnosticForMultipleLinesCommentedCode()
        {
            var test = _(@"
            // if (something)
            // {
            //   DoStuff();
            // }");
            var expected = new DiagnosticResult
            {
                Id = RemoveCommentedCodeAnalyzer.DiagnosticId,
                Message = "If code is commented, it should be removed.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 13) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task RemovesCommentedMultilineCodePreservingRegularComments()
        {
            var test = _(@"
            // this comment will be preserved
            // if (something)
            // {
            //   DoStuff();
            // }
            "
            );

            var fixtest = _(@"
            // this comment will be preserved
            "
            );
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task RemovesNonPerfectClassCommentedCode()
        {
            var test = _(@"
            // this comment will be preserved
            // class Fee
            class Foo
            {
            }
            "
            );

            var fixtest = _(@"
            // this comment will be preserved
            class Foo
            {
            }
            "
            );
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact]
        public async Task RemovesNonPerfectIfCommentedCode()
        {
            var test = _(@"
            // this comment will be preserved
            // if (a > 2)
            if (a > 3)
            {
            }
            "
            );

            var fixtest = _(@"
            // this comment will be preserved
            if (a > 3)
            {
            }
            "
            );
            await VerifyCSharpFixAsync(test, fixtest);
        }

        private string _(string code)
        {
            return @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                " + code + @"
            }
        }
    }";
        }
    }
}
