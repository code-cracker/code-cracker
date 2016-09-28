using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class RemoveAtFromVariablesTests : CodeFixVerifier<RemoveAtFromVariablesAnalyzer, RemoveAtFromVariablesCodeFixProvider>
    {
        [Theory]
        [InlineData("bool")]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("double")]
        [InlineData("float")]
        [InlineData("decimal")]
        [InlineData("string")]
        [InlineData("char")]
        [InlineData("object")]
        [InlineData("typeof")]
        [InlineData("sizeof")]
        [InlineData("null")]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("if")]
        [InlineData("else")]
        [InlineData("while")]
        [InlineData("for")]
        [InlineData("foreach")]
        [InlineData("do")]
        [InlineData("switch")]
        [InlineData("case")]
        [InlineData("default")]
        [InlineData("lock")]
        [InlineData("try")]
        [InlineData("throw")]
        [InlineData("catch")]
        [InlineData("finally")]
        [InlineData("goto")]
        [InlineData("break")]
        [InlineData("continue")]
        [InlineData("return")]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("static")]
        [InlineData("readonly")]
        [InlineData("sealed")]
        [InlineData("const")]
        [InlineData("new")]
        [InlineData("override")]
        [InlineData("abstract")]
        [InlineData("virtual")]
        [InlineData("partial")]
        [InlineData("ref")]
        [InlineData("out")]
        [InlineData("in")]
        [InlineData("where")]
        [InlineData("params")]
        [InlineData("this")]
        [InlineData("base")]
        [InlineData("namespace")]
        [InlineData("using")]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("delegate")]
        [InlineData("checked")]
        [InlineData("get")]
        [InlineData("set")]
        [InlineData("add")]
        [InlineData("remove")]
        [InlineData("operator")]
        [InlineData("implicit")]
        [InlineData("explicit")]
        [InlineData("fixed")]
        [InlineData("extern")]
        [InlineData("event")]
        [InlineData("enum")]
        [InlineData("unsafe")]
        public async Task IgnoresFieldDeclarationsUsingAtSymbolForKeywords(string value)
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class Foo
        {
            string @" + value + @"= ""Foo"";
            public async Task Bar()
            {
                
            }
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Theory]
        [InlineData("bool")]
        [InlineData("byte")]
        [InlineData("sbyte")]
        [InlineData("short")]
        [InlineData("ushort")]
        [InlineData("int")]
        [InlineData("uint")]
        [InlineData("long")]
        [InlineData("ulong")]
        [InlineData("double")]
        [InlineData("float")]
        [InlineData("decimal")]
        [InlineData("string")]
        [InlineData("char")]
        [InlineData("object")]
        [InlineData("typeof")]
        [InlineData("sizeof")]
        [InlineData("null")]
        [InlineData("true")]
        [InlineData("false")]
        [InlineData("if")]
        [InlineData("else")]
        [InlineData("while")]
        [InlineData("for")]
        [InlineData("foreach")]
        [InlineData("do")]
        [InlineData("switch")]
        [InlineData("case")]
        [InlineData("default")]
        [InlineData("lock")]
        [InlineData("try")]
        [InlineData("throw")]
        [InlineData("catch")]
        [InlineData("finally")]
        [InlineData("goto")]
        [InlineData("break")]
        [InlineData("continue")]
        [InlineData("return")]
        [InlineData("public")]
        [InlineData("private")]
        [InlineData("internal")]
        [InlineData("protected")]
        [InlineData("static")]
        [InlineData("readonly")]
        [InlineData("sealed")]
        [InlineData("const")]
        [InlineData("new")]
        [InlineData("override")]
        [InlineData("abstract")]
        [InlineData("virtual")]
        [InlineData("partial")]
        [InlineData("ref")]
        [InlineData("out")]
        [InlineData("in")]
        [InlineData("where")]
        [InlineData("params")]
        [InlineData("this")]
        [InlineData("base")]
        [InlineData("namespace")]
        [InlineData("using")]
        [InlineData("class")]
        [InlineData("struct")]
        [InlineData("interface")]
        [InlineData("delegate")]
        [InlineData("checked")]
        [InlineData("get")]
        [InlineData("set")]
        [InlineData("add")]
        [InlineData("remove")]
        [InlineData("operator")]
        [InlineData("implicit")]
        [InlineData("explicit")]
        [InlineData("fixed")]
        [InlineData("extern")]
        [InlineData("event")]
        [InlineData("enum")]
        [InlineData("unsafe")]
        public async Task IgnoresDeclarationsUsingAtSymbolForKeywords(string value)
        {
            var test = @"
    using System;

    namespace ConsoleApplication1
    {
        class Foo
        {
            public async Task Bar()
            {
                string @" + value + @"= ""Foo"";
            }
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(test);

        }

        [Fact]
        public async Task DeclarationsTypePrimitiveWithAtSymbolOnNameButNotKeywordsCreatesDiagnosic()
        {
            const string test = @"
    using System;
    namespace ConsoleApplication1
    {
        class Foo
        {
            void Bar()
            {
                string @x = ""Me"";
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords.ToDiagnosticId(),
                Message = "Remove @ from variables that are not keywords.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task DeclarationsWithAtSymbolOnNameButNotKeywordsCreatesDiagnosic()
        {
            const string test = @"
    using System;
    namespace ConsoleApplication1
    {
        class Foo
        {
            void Bar()
            {
                var @x = ""Me"";
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords.ToDiagnosticId(),
                Message = "Remove @ from variables that are not keywords.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 17) }
            };
            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task FixReplacesRemovingAtSymbolFromNotKeywordsVariableNames()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class Foo
        {
            public async Task Bar()
            {
                int @a = 10;
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class Foo
        {
            public async Task Bar()
            {
                int a = 10;
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }


    }
}
