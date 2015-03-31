using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.Diagnostics;
using CodeCracker.CSharp.Usage;

namespace CodeCracker.Test.CSharp.Usage
{
    public class StringFormatTests : CodeFixVerifier
    {
        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() => new StringFormatArgsAnalyzer();

        [Fact]
        public async Task IgnoresRegularStrings()
        {
            var source = @"var string a = ""a"";".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresStringMethodsThatAreNotStringFormat()
        {
            var source = @"var result = string.Compare(""a"", ""b"");".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsCalledFormatThatAreNotStringFormat()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class OtherString { public static string Format(string a, string b) { throw new NotImplementedException(); } }
        class TypeName
        {
            void Foo()
            {
                var result = OtherString.Format(""a"", ""b"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresStringFormatWithArrayArgWith1Object()
        {
            var source = @"
                var noun = ""Giovanni"";
                var args = new object[] { noun };
                var s = string.Format(""This {0} is nice."", args);".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresStringFormatWithArrayArgWith2Objects()
        {
            var source = @"
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var args = new object[] { noun, adjective };
                var s = string.Format(""This {0} is {1}"", args);".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsWithOnlyOneParameterAndNoFormatHole()
        {
            var source = @"var result = string.Format(""a"");".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsCalledWithIncorrectParameterTypes()
        {
            var source = @"var result = string.Format(1, ""b"");".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task MethodsWithLessParametersCreatesDiagnostic()
        {
            var source = @"var result = string.Format(""one {0} two {1}"", ""a"");".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringFormatArgs.ToDiagnosticId(),
                Message = StringFormatArgsAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 30) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task MethodsWithMoreParametersCreatesDiagnostic()
        {
            var source = @"var result = string.Format(""one {0} two {1}"", ""a"", ""b"", ""c"");".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringFormatArgs.ToDiagnosticId(),
                Message = StringFormatArgsAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 30) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IgnoreStringFormatWithCorrectNumberOfParameters()
        {
            var source = @"
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = System.String.Format(""This {0} is {1}"", noun, adjective);".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreVerbatimStringWithCorrectNumberOfHoles()
        {
            var source = @"
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var s = string.Format(@""This {0} is
""""{1}""""."", noun, adjective);".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task VerbatimStringWithIncorrectNumberOfHolesCreatesDiagnostic()
        {
            var source = @"
                var noun = ""Giovanni"";
                var s = string.Format(@""This {0} is
""""{1}""""."", noun);".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringFormatArgs.ToDiagnosticId(),
                Message = StringFormatArgsAnalyzer.MessageFormat,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 25) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }
    }
}