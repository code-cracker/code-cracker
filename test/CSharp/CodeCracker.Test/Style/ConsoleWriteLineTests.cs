using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class ConsoleWriteLineTests : CodeFixVerifier<ConsoleWriteLineAnalyzer, ConsoleWriteLineCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresRegularStrings()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var string a = ""a"";
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresConsoleMethodsThatAreNotConsoleWriteLine()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                Console.Write(1);
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsCalledWriteLineThatAreNotConsoleWriteLine()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class OtherString { public static string WriteLine(string a, string b) { throw new NotImplementedException(); } }
        class TypeName
        {
            void Foo()
            {
                var result = OtherString.WriteLine(""a"", ""b"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresConsoleWriteLineWithArrayArgWith1Object()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var args = new object[] { noun };
                Console.WriteLine(""This {0} is nice."", args);
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresConsoleWriteLineWithArrayArgWith2Objects()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var args = new object[] { noun, adjective };
                Console.WriteLine(""This {0} is {1}"", args);
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsWithOnlyOneParameter()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                Console.WriteLine(""a"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsCalledWithIncorrectParameterTypes()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                Console.WriteLine(1, ""b"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresMethodsWithIncorrectNumberOfParameters()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                Console.WriteLine(""one {0} two {1}"", ""a"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConsoleWriteLineWithMoreThan2ArgsProducesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine(""This {0} is {1}"", noun, adjective);
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ConsoleWriteLine.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(11, 17)
                .WithMessage(ConsoleWriteLineAnalyzer.MessageFormat.ToString());
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ConsoleWriteLineWithFullStringNameProducesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                System.Console.WriteLine(""This {0} is {1}"", noun, adjective);
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ConsoleWriteLine.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 17)
                .WithMessage(ConsoleWriteLineAnalyzer.MessageFormat.ToString());
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ConsoleWriteLineChangesToStringInterpolation()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                //comment before
                Console.WriteLine(""This {0} is {1}"", noun, adjective);//comment right after
                //comment after
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                //comment before
                Console.WriteLine($""This {noun} is {adjective}"");//comment right after
                //comment after
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare:false);
        }

        [Fact]
        public async Task WhenConsoleWriteLineHasMoreArgumentsThanNecessaryChangesToStringInterpolationAndIgnoresExtraArgument()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var otherAdjective = ""loves c#"";
                Console.WriteLine(""This {0} is {1}"", noun, adjective, otherAdjective);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                var otherAdjective = ""loves c#"";
                Console.WriteLine($""This {noun} is {adjective}"");
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected, allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public async Task WhenConsoleWriteLineHasEscapingItChangesToStringInterpolationAndRemovesEscapingSequence()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine(""This {{0}} {0} is {1}"", noun, adjective);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine($""This {{0}} {noun} is {adjective}"");
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenFormatStringHasLineBreaksTheCodeFixKeepsThat()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine(""This {0} is\n \r\f \f \r {1}"", noun, adjective);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine($""This {noun} is\n \r\f \f \r {adjective}"");
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task WhenFormatStringHasQuotesTheCodeFixKeepsThat()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine(""This {0} is \""{1}\"""", noun, adjective);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine($""This {noun} is \""{adjective}\"""");
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task VerbatimStringWithConsoleWriteLineCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine(@""This {0} is
""""{1}""""."", noun, adjective);
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ConsoleWriteLine.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(11, 17)
                .WithMessage(ConsoleWriteLineAnalyzer.MessageFormat.ToString());
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task VerbatimStringBecomesInterpolatedVerbatimString()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine(@""This {0} is
""""{1}""""."", noun, adjective);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine($@""This {noun} is
""""{adjective}""""."");
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FormatStringMaintainsFormat()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                Console.WriteLine("" |{0, -15 :N5}| "", System.Math.PI);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                Console.WriteLine($"" |{System.Math.PI, -15 :N5}| "");
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare:false);
        }

        [Fact]
        public async Task ConsoleWriteLineChangesToStringInterpolationWithInvertedIndexes()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                //comment before
                Console.WriteLine(""This {1} is {0}"", noun, adjective);//comment right after
                //comment after
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                //comment before
                Console.WriteLine($""This {adjective} is {noun}"");//comment right after
                //comment after
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare:false);
        }

        [Fact]
        public async Task ConsoleWriteLineWithTernaryOperatorFixesWithParenthesis()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine(""This {0} is {1}"", noun, true ? adjective : noun);
            }
        }
    }";
            const string expected = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            void Foo()
            {
                var noun = ""Giovanni"";
                var adjective = ""smart"";
                Console.WriteLine($""This {noun} is {(true ? adjective : noun)}"");
            }
        }
    }";
            await VerifyCSharpFixAsync(source, expected, formatBeforeCompare:false);
        }
    }
}