using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using Xunit;
using Xunit.Extensions;

namespace CodeCracker.Test.CSharp.Usage
{
    public class ValidateColorTests : CodeFixVerifier
    {
        private static IEnumerable<object[]> KnownColorNames
        {
            get
            {
                var result = new List<object[]>();
                foreach (var knownColor in Enum.GetValues(typeof(ValidateColorAnalyzer.KnownColor)))
                {
                    result.Add(new object[] { knownColor.ToString() });
                }
                return result;
            }
        }

        private static IEnumerable<object[]> AlternativeSystemColorNames
        {
            get
            {
                var result = new List<object[]>();
                // special case for Color.LightGray versus html's LightGrey (#340917)
                result.Add(new object[] { "lightgrey" });
                result.Add(new object[] { "captiontext" });
                result.Add(new object[] { "threeddarkshadow" });
                result.Add(new object[] { "threedhighlight" });
                result.Add(new object[] { "background" });
                result.Add(new object[] { "buttontext" });
                result.Add(new object[] { "infobackground" });
                return result.ToArray();
            }
        }

        public static IEnumerable<object[]> AllColorNames
        {
            get
            {
                var result = new List<object[]>();
                result.AddRange(KnownColorNames);
                result.AddRange(AlternativeSystemColorNames);
                return result.ToArray();
            }
        }

        [Theory]
        [MemberData(nameof(AllColorNames))]
        [InlineData("")]
        [InlineData("#FFFFFF")]
        [InlineData("#DEB887")]
        [InlineData("0xF")]
        [InlineData("200")]
        [InlineData("200,100,50")]
        [InlineData("200,100,50,60")]
        public async Task WhenUsingValidColorAnalyzerDoesNotCreateDiagnostic(string htmlColor)
        {
            var source = @"
            namespace ConsoleApplication1
            {
                class TypeName
                {
                    public int Foo()
                    {
                        var color = System.Drawing.ColorTranslator.FromHtml(""" + htmlColor + @""");
                    }
                }
            }";
            using (new ChangeCulture(""))
                await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [InlineData("#FFFFFF")]
        [InlineData("#DEB887")]
        [InlineData("Red")]
        [InlineData("wrong color")]
        public async Task WhenNotUsingLiteralExpressionSyntaxColorValueAnalyzerDoesNotCreateDiagnostic(string htmlColor)
        {
            var source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var colorArg = """ + htmlColor + @""";
                ColorTranslator.FromHtml(colorArg);
            }
        }
    }";
            using (new ChangeCulture(""))
                await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [InlineData("#FFFZZZ")]
        [InlineData("bluee")]
        [InlineData("wrong color")]
        [InlineData("200,100")]
        [InlineData("200,JJ,50,60")]
        [InlineData("200,100,50,100,60")]
        [InlineData("-300, 100, 100")]
        [InlineData("300, 100, 100")]
        [InlineData("100, -300, 100")]
        [InlineData("100, 300, 100")]
        [InlineData("100, 100, -300")]
        [InlineData("100, 100, 300")]
        [InlineData("-300, 100, 100, 100")]
        [InlineData("300, 100, 100, 100")]
        public async Task WhenUsingInvalidColorAnalyzerCreatesCreateDiagnostic(string htmlColor)
        {
            var source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var color = ColorTranslator.FromHtml(""" + htmlColor + @""");
            }
        }
    }";
            using (new ChangeCulture(""))
                await VerifyCSharpDiagnosticAsync(source, CreateDiagnosticResult(8, 29));
        }

        public static DiagnosticResult CreateDiagnosticResult(int line, int column)
        {
            return new DiagnosticResult
            {
                Id = DiagnosticId.ValidateColor.ToDiagnosticId(),
                Message = ValidateColorAnalyzer.Message.ToString(),
                Severity = DiagnosticSeverity.Error,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", line, column) }
            };
        }

        protected override DiagnosticAnalyzer GetDiagnosticAnalyzer() => new ValidateColorAnalyzer();
    }
}