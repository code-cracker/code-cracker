using CodeCracker.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Performance
{
    public class StringBuilderInLoopTests : CodeFixTest<StringBuilderInLoopAnalyzer, StringBuilderInLoopCodeFixProvider>
    {

        [Fact]
        public async Task WhileWithoutAddAssignmentExpressionDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                while (false)
                {
                    Method();
                }
            }
            public void Method() { }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }


        [Fact]
        public async Task WhileWithoutStringConcatDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var a = 0;
                while (a < 10)
                {
                    a += 1;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhileWithtStringConcatOnLocalVariableCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                while (true)
                {
                    myString += """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = StringBuilderInLoopAnalyzer.DiagnosticId,
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhileWithtStringConcatOnFieldVariableCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private string myString = """";
            public void Foo()
            {
                while (true)
                {
                    myString += """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = StringBuilderInLoopAnalyzer.DiagnosticId,
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }


        [Fact]
        public async Task WhileWithtStringConcatOnPropertyVariableCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string MyString { get; set; } = """";
            public void Foo()
            {
                while (true)
                {
                    MyString += """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = StringBuilderInLoopAnalyzer.DiagnosticId,
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "MyString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhileWithtStringConcatWithSeveralConcatsOnDifferentVarsCreatesSeveralDiagnostics()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString1 = """";
                var myString2 = """";
                while (true)
                {
                    myString1 += """";
                    myString2 += """";
                }
            }
        }
    }";
            var expected1 = new DiagnosticResult
            {
                Id = StringBuilderInLoopAnalyzer.DiagnosticId,
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString1"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 21) }
            };
            var expected2 = new DiagnosticResult
            {
                Id = StringBuilderInLoopAnalyzer.DiagnosticId,
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString2"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task WhileWithtStringConcatWithSimpleAssignmentCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                while (true)
                {
                    myString = myString + """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = StringBuilderInLoopAnalyzer.DiagnosticId,
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhileWithtStringConcatWithSimpleAssignmentOnDifferentVarDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                var otherString = """";
                while (true)
                {
                    myString = otherString + """";
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FixesAddAssignmentInWhile()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                while (true)
                {
                    myString += ""a"";
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                while (true)
                {
                    builder.Append(""a"");
                }
                myString = builder.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task FixesAddAssignmentInWhileWithoutBlock()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                while (true)
                    myString += ""a"";
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                while (true)
                    builder.Append(""a"");
                myString = builder.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task FixesAddAssignmentInWhileWithSystemTextInContext()
        {
            const string source = @"
    using System.Text;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                while (true)
                {
                    myString += ""a"";
                }
            }
        }
    }";
            const string fixtest = @"
    using System.Text;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                var builder = new StringBuilder();
                builder.Append(myString);
                while (true)
                {
                    builder.Append(""a"");
                }
                myString = builder.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task FixesSimpleAssignmentInWhile()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                //comment 3
                while (true)
                {
                    //comment 1
                    myString = myString + ""a"";//comment 2
                }//comment 4
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                //comment 3
                while (true)
                {
                    //comment 1
                    builder.Append(""a"");//comment 2
                }//comment 4
                myString = builder.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task FixesAddAssignmentWhenThereAre2WhilesOnBlock()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                while (false)
                {
                    var a = 1;
                }
                while (true)
                {
                    myString += ""a"";
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                while (false)
                {
                    var a = 1;
                }
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                while (true)
                {
                    builder.Append(""a"");
                }
                myString = builder.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task FixesAddAssignmentWithoutClashingTheBuilderName()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var builder = 1;
                var myString = """";
                while (true)
                {
                    myString += ""a"";
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var builder = 1;
                var myString = """";
                var builder1 = new System.Text.StringBuilder();
                builder1.Append(myString);
                while (true)
                {
                    builder1.Append(""a"");
                }
                myString = builder1.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task FixesAddAssignmentWithoutClashingTheBuilderNameOnAField()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int builder1;
            public void Foo()
            {
                var builder = 1;
                var myString = """";
                while (true)
                {
                    myString += ""a"";
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int builder1;
            public void Foo()
            {
                var builder = 1;
                var myString = """";
                var builder2 = new System.Text.StringBuilder();
                builder2.Append(myString);
                while (true)
                {
                    builder2.Append(""a"");
                }
                myString = builder2.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task ForWithtStringConcatOnLocalVariableCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                for (;;)
                {
                    myString += """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = StringBuilderInLoopAnalyzer.DiagnosticId,
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixesAddAssignmentInFor()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                for (;;)
                {
                    myString += ""a"";
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                for (;;)
                {
                    builder.Append(""a"");
                }
                myString = builder.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task ForeachWithtStringConcatOnLocalVariableCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                foreach (var i in new [] {1,2,3})
                {
                    myString += """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = StringBuilderInLoopAnalyzer.DiagnosticId,
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixesAddAssignmentInForeach()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                foreach (var i in new [] {1,2,3})
                {
                    myString += ""a"";
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                foreach (var i in new [] {1,2,3})
                {
                    builder.Append(""a"");
                }
                myString = builder.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task DoWithtStringConcatOnLocalVariableCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                do
                {
                    myString += """";
                } while (true);
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = StringBuilderInLoopAnalyzer.DiagnosticId,
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixesAddAssignmentInDo()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                do
                {
                    myString += ""a"";
                } while (true);
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                do
                {
                    builder.Append(""a"");
                } while (true);
                myString = builder.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }
    }
}