using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class ValidateColorTests : CodeFixVerifier
    {
        [Theory]
        [InlineData("#FFFFFF")]
        [InlineData("#DEB887")]
        [InlineData("Red")]
        [InlineData("blue")]
        [InlineData("BLUE")]
        [InlineData("WhITe")]
        [InlineData("ActiveCaptionText")]
        [InlineData("")]
        [InlineData("200")]
        [InlineData("200;100;50")]
        [InlineData("200;100;50;60")]
        public async Task WhenUsingValidColorAnalyzerDoesNotCreateDiagnostic(string htmlColor)
        {
            var htmlColorWithCorrectListSeparator = ReplaceListSeparator(htmlColor, ";");
            var source = @"
            namespace ConsoleApplication1
            {
                class TypeName
                {
                    public int Foo()
                    {
                        var color = ColorTranslator.FromHtml(""" + htmlColorWithCorrectListSeparator + @""");
                    }
                }
            }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [InlineData("#FFFFFF")]
        [InlineData("#DEB887")]
        [InlineData("Red")]
        [InlineData("wrong color")]
        public async Task WhenNotUsingLiteralExpressionSyntaxColorValueAnalyzerDoesNotCreateDiagnostic(string htmlColor)
        {
            var htmlColorWithCorrectListSeparator = ReplaceListSeparator(htmlColor, ";");
            var source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var colorArg = """ + htmlColorWithCorrectListSeparator + @""";
                ColorTranslator.FromHtml(colorArg);
            }
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [InlineData("#FFFZZZ")]
        [InlineData("bluee")]
        [InlineData("wrong color")]
        [InlineData("200;100")]
        [InlineData("200;JJ;50;60")]
        [InlineData("200;100;50;100;60")]
        public async Task WhenUsingInvalidColorAnalyzerCreatesCreateDiagnostic(string htmlColor)
        {
            var htmlColorWithCorrectListSeparator = ReplaceListSeparator(htmlColor, ";");
            var source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var color = ColorTranslator.FromHtml(""" + htmlColorWithCorrectListSeparator + @""");
            }
        }
    }";

            await VerifyCSharpDiagnosticAsync(source, CreateDiagnosticResult(8, 29));
        }

        public static DiagnosticResult CreateDiagnosticResult(int line, int column)
        {
            return new DiagnosticResult
            {
                Id = DiagnosticId.ValidateColor.ToDiagnosticId(),
                Message = ValidateColorAnalyzer.Message,
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, column) }
            };
        }

        private static string ReplaceListSeparator(string htmlColor, string currentListSeparator)
        {
            var numSeparator = CultureInfo.CurrentCulture.TextInfo.ListSeparator;
            return htmlColor.Replace(currentListSeparator, numSeparator);
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() => new ValidateColorAnalyzer();
    }
}