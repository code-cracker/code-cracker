using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class UnnecessaryToStringInStringConcatenationTests : CodeFixVerifier<UnnecessaryToStringInStringConcatenationAnalyzer, UnnecessaryToStringInStringConcatenationCodeFixProvider>
    {
        [Fact]
        public async Task InstantiatingAnObjectAndCallToStringInsideAStringConcatenationShouldGenerateDiagnosticResult()
        {
            const string source = @"var foo = ""a"" + new object().ToString();";

            var expected = CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(1, 29);

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task InstantiatingAnStringBuilderAndCallToStringInsideAStringConcatenationShouldGenerateDiagnosticResult()
        {
            const string source = @"var foo = ""a"" + new System.Text.StringBuilder().ToString();";

            var expected = CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(1, 48);

            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task CallToStringForAnInstantiatedObjectInsideAStringConcatenationShouldGenerateDiagnosticResult()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class AuxClass
        {
            public override void ToString()
            {
                return ""Test""; 
            }
        }

        class TypeName
        {
            public void Foo()
            {
                var auxClass = new AuxClass();

                var bar = ""a"" + new AuxClass().ToString();
                var foo = ""a"" + auxClass.ToString();
                var far = ""a"" + new AuxClass().ToString() + auxClass.ToString() + new object().ToString(""C"");
            }
        }
    }";

            var expected1 = CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(20, 47);
            var expected2 = CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(21, 41);
            var expected3 = CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(22, 47);
            var expected4 = CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(22, 69);

            var expected = new DiagnosticResult[] { expected1, expected2, expected3, expected4 };

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CallToStringInsideAStringConcatenationWithAFormatParameterShouldNotGenerateDiagnosticResult()
        {
            const string source = @"var salary = ""salary: "" + 1000.ToString(""C"");";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }


        [Fact]
        public async Task CallToStringOutsideAStringConcatenationWithoutParameterShouldNotGenerateDiagnosticResult()
        {
            const string source = @"var value = 1000.ToString();";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }


        private static DiagnosticResult CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(int expectedRow, int expectedColumn)
        {
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UnnecessaryToStringInStringConcatenation.ToDiagnosticId(),
                Message = "Unnecessary '.ToString()' call in string concatenation.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", expectedRow, expectedColumn) }
            };

            return expected;
        }
    }
}