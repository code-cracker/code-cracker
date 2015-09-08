﻿using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
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

        [Fact(Skip ="Skipped until SourceCodeKind.Interactive can be set on CSharpParseOptions on the analyzer.")]
        public async Task CreateDiagnosticForSingleLineCommentedCode()
        {
            var test = @"// a = 10;".WrapInCSharpMethod();
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
            ".WrapInCSharpMethod();
            var fixtest = @"
            // this comment will be preserved
            ".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(test, fixtest);
        }

        [Fact(Skip ="Skipped until SourceCodeKind.Interactive can be set on CSharpParseOptions on the analyzer.")]
        public async Task CreateDiagnosticForMultipleLinesCommentedCode()
        {
            var test = @"
            // if (something)
            // {
            //   DoStuff();
            // }".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.RemoveCommentedCode.ToDiagnosticId(),
                Message = RemoveCommentedCodeAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 13) }
            };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact(Skip ="Skipped until SourceCodeKind.Interactive can be set on CSharpParseOptions on the analyzer.")]
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

        [Fact(Skip ="Skipped until SourceCodeKind.Interactive can be set on CSharpParseOptions on the analyzer.")]
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