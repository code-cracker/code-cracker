using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using Microsoft.CodeAnalysis.Diagnostics;
using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis.Testing;

namespace CodeCracker.Test.CSharp.Usage
{
    public class StringFormatArgsTests : CodeFixVerifier
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
        public async Task NoParametersCreatesError()
        {
            var source = @"var result = string.Format(""{0}"");".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.StringFormatArgs_InvalidArgs.ToDiagnosticId(), DiagnosticSeverity.Error)
                .WithLocation(10, 26)
                .WithMessage(StringFormatArgsAnalyzer.InvalidArgsReferenceMessage);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task LessParametersCreatesError()
        {
            var source = @"var result = string.Format(""one {0} two {1}"", ""a"");".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.StringFormatArgs_InvalidArgs.ToDiagnosticId(), DiagnosticSeverity.Error)
                .WithLocation(10, 26)
                .WithMessage(StringFormatArgsAnalyzer.InvalidArgsReferenceMessage);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task MoreArgumentsCreatesWarning()
        {
            var source = @"var result = string.Format(""one {0} two {1}"", ""a"", ""b"", ""c"");".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.StringFormatArgs_ExtraArgs.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(10, 26)
                .WithMessage(StringFormatArgsAnalyzer.IncorrectNumberOfArgsMessage);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task MethodWithStringInterpolationDoesNotCreateDiagnostic()
        {
            var source = @"var result = string.Format($""one {{0}} two {{1}} {""whatever""}"", ""a"", ""b"", ""c"");".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task MethodWithParamtersReferencingSingleArgumentDoesNotCreateDiagnostic()
        {
            var source = @"var result = string.Format(""one {0} two {0}"", ""a"");".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task MethodWithParamtersReferencingSingleAndFormatSpecifiersArgumentDoesNotCreateDiagnostic()
        {
            var source = @"var result = string.Format(""PI {0:0.##} PI as Percent {0:P}"", Math.PI);".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task TwoParametersReferencingSamePlaceholderCreatesWarning()
        {
            var source = @"var result = string.Format(""one {0} two {0}"", ""a"", ""b"");".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.StringFormatArgs_ExtraArgs.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(10, 26)
                .WithMessage(StringFormatArgsAnalyzer.IncorrectNumberOfArgsMessage);
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
        public async Task VerbatimStringWithMissingArgCreatesError()
        {
            var source = @"
                var noun = ""Giovanni"";
                var s = string.Format(@""This {0} is
""""{1}""""."", noun);".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.StringFormatArgs_InvalidArgs.ToDiagnosticId(), DiagnosticSeverity.Error)
                .WithLocation(12, 25)
                .WithMessage(StringFormatArgsAnalyzer.InvalidArgsReferenceMessage);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task InvalidArgumentReferenceCreatesError()
        {
            var source = @"var result = string.Format(""one {1}"", ""a"");".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.StringFormatArgs_InvalidArgs.ToDiagnosticId(), DiagnosticSeverity.Error)
                .WithLocation(10, 26)
                .WithMessage(StringFormatArgsAnalyzer.InvalidArgsReferenceMessage);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task NonIntegerPlaceholderCreatesError()
        {
            var source = @"var result = string.Format(""one {notZero}"", ""a"");".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.StringFormatArgs_InvalidArgs.ToDiagnosticId(), DiagnosticSeverity.Error)
                .WithLocation(10, 26)
                .WithMessage(StringFormatArgsAnalyzer.InvalidArgsReferenceMessage);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task UnusedArgsCreatesWarning()
        {
            var source = @"string.Format(""{0}{1}{3}{5}"", ""a"", ""b"", ""c"", ""d"", ""e"", ""f"");".WrapInCSharpMethod();
            var expected = new DiagnosticResult(DiagnosticId.StringFormatArgs_ExtraArgs.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(10, 13)
                .WithMessage(StringFormatArgsAnalyzer.IncorrectNumberOfArgsMessage);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }
    }
}