using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    [System.Diagnostics.CodeAnalysis.SuppressMessage(nameof(Style), "CC0065", Justification = "This is the remove trailing whitespace test class.")]
    public class RemoveTrailingWhitespaceTests : CodeFixVerifier<RemoveTrailingWhitespaceAnalyzer, RemoveTrailingWhitespaceCodeFixProvider>
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
"";".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        public static DiagnosticResult CreateDiagnostic(int line, int column)
        {
            return new DiagnosticResult(DiagnosticId.RemoveTrailingWhitespace.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(line, column)
                .WithMessage(RemoveTrailingWhitespaceAnalyzer.MessageFormat);
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