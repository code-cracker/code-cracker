using CodeCracker.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Style
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "CC0065", Justification = "This is the remove trailing whitespace test class.")]
    public class RemoveTrailingWhitespaceTests : CodeFixTest<RemoveTrailingWhitespaceAnalyzer, RemoveTrailingWhitespaceCodeFixProvider>
    {
        [Fact]
        public async Task SingleStatementWithTrailingSpaceCreatesDiagnostic()
        {
            const string source = "using System; ";
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnostic(1, 13));
        }

        [Fact]
        public async Task NoTrailingWhitespaceDoesNotCreateDiagnostic()
        {
            const string source = "using System;\r\n";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task LineWithSpaceDoesNotCreateDiagnostic()
        {
            const string source = "\r\n";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task SingleStatementWithTrailingSpaceAndNewLineCreatesOnlyOneDiagnostic()
        {
            const string source = "using System; \r\n";
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnostic(1, 13));
        }

        [Fact]
        public async Task TwoStatementsWithTrailingWhiteSpaceCreatesTwoDiagnostics()
        {
            const string source = @"using System; 
namespace Foo 
{}";
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnostic(1, 13), CreateDiagnostic(2, 14));
        }

        [Fact]
        public async Task RemoveTrailingWhiteSpace()
        {
            const string source = "using System; \r\n";
            const string expected = "using System;\r\n";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare: false);
        }

        [Fact]
        public async Task RemoveTrailingSpaceWithoutNewLine()
        {
            const string source = "using System; ";
            const string expected = "using System;";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare: false);
        }

        [Fact]
        public async Task RemoveTrailingTabWithoutNewLine()
        {
            const string source = "using System;	";
            const string expected = "using System;";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare: false);
        }

        [Fact]
        public async Task SingleStatementWithCommentAndTrailingSpaceCreatesDiagnostic()
        {
            const string source = "using System;//a ";
            await VerifyCSharpDiagnosticAsync(source, CreateDiagnostic(1, 16));
        }

        [Fact]
        public async Task FixCommentTrailingSpaceAndNewLine()
        {
            const string source = "using System;//a \r\n";
            const string expected = "using System;//a\r\n";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare: false);
        }

        [Fact]
        public async Task StringWithTrailingWhitespaceDoesNotCreateDiagnostic()
        {
            var source = @"var s = @"" 
"";".WrapInMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        public DiagnosticResult CreateDiagnostic(int line, int column)
        {
            var diagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.RemoveTrailingWhitespace.ToDiagnosticId(),
                Message = RemoveTrailingWhitespaceAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, column) }
            };
            return diagnostic;
        }

        [Fact]
        public async Task FixSingleLineXmlComments()
        {
            const string source = @"
/// <summary>
/// Comment 
/// </summary>
class Foo { }";
            const string expected = @"
/// <summary>
/// Comment
/// </summary>
class Foo { }";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare: false);
        }

        [Fact]
        public async Task FixMultiLineXmlComments()
        {
            const string source = @"
/**
* <summary>
* Comment 
* </summary>
*/
class Foo { }";
            const string expected = @"
/**
* <summary>
* Comment
* </summary>
*/
class Foo { }";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare: false);
        }
    }
}