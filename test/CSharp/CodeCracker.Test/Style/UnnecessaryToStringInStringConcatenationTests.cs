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
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.UnnecessaryToStringInStringConcatenation.ToDiagnosticId(),
                Message = "Unnecessary ToString code should be removed.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 1, 29) }
            };

            await VerifyCSharpDiagnosticAsync(source, expected);
        }
    }
}