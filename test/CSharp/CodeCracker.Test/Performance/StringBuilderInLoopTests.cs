﻿using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class StringBuilderInLoopTests : CodeFixVerifier<StringBuilderInLoopAnalyzer, StringBuilderInLoopCodeFixProvider>
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
                while (DateTime.Now.Second % 2 == 0)
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
            var source = @"
                var a = 0;
                while (a < 10)
                {
                    a += 1;
                }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhileWithoutStringConcatWithMethoParameterDoesNotCreateDiagnostic()
        {
            var source = @"
    public class TypeName
    {
        public void Looper(ref int a = 0)
        {
            while (a < 10)
            {
                a += 1;
            }
        }
    }";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhileWithStringConcatOnLocalVariableCreatesDiagnostic()
        {
            var source = @"
                var myString = """";
                while (DateTime.Now.Second % 2 == 0)
                {
                    myString += """";
                }".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhileWithStringConcatOnFieldVariableCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private string myString = """";
            public void Foo()
            {
                while (DateTime.Now.Second % 2 == 0)
                {
                    myString += """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }


        [Fact]
        public async Task WhileWithStringConcatOnPropertyVariableCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public string MyString { get; set; } = """";
            public void Foo()
            {
                while (DateTime.Now.Second % 2 == 0)
                {
                    MyString += """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "MyString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 11, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhileWithStringConcatWithSeveralConcatsOnDifferentVarsCreatesSeveralDiagnostics()
        {
            var source = @"
                var myString1 = """";
                var myString2 = """";
                while (DateTime.Now.Second % 2 == 0)
                {
                    myString1 += """";
                    myString2 += """";
                }".WrapInCSharpMethod();
            var expected1 = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString1"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 21) }
            };
            var expected2 = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString2"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task WhileWithStringConcatWithSimpleAssignmentCreatesDiagnostic()
        {
            var source = @"
                var myString = """";
                while (DateTime.Now.Second % 2 == 0)
                {
                    myString = myString + """";
                }".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhileWithStringConcatWithSimpleAssignmentOnDifferentVarDoesNotCreateDiagnostic()
        {
            var source = @"
                var myString = """";
                var otherString = """";
                while (DateTime.Now.Second % 2 == 0)
                {
                    myString = otherString + """";
                }".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FixesAddAssignmentInWhile()
        {
            var source = @"
                var myString = """";
                while (DateTime.Now.Second % 2 == 0)
                {
                    myString += ""a"";
                }".WrapInCSharpMethod();
            var fixtest = @"
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                while (DateTime.Now.Second % 2 == 0)
                {
                    builder.Append(""a"");
                }
                myString = builder.ToString();".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixesAddAssignmentInWhileWithoutBlock()
        {
            var source = @"
                var myString = """";
                while (DateTime.Now.Second % 2 == 0)
                    myString += ""a"";".WrapInCSharpMethod();
            var fixtest = @"
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                while (DateTime.Now.Second % 2 == 0)
                    builder.Append(""a"");
                myString = builder.ToString();".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
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
                while (DateTime.Now.Second % 2 == 0)
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
                while (DateTime.Now.Second % 2 == 0)
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
            var source = @"
                var myString = """";
                //comment 3
                while (DateTime.Now.Second % 2 == 0)
                {
                    //comment 1
                    myString = myString + ""a"";//comment 2
                }//comment 4".WrapInCSharpMethod();
            var fixtest = @"
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                //comment 3
                while (DateTime.Now.Second % 2 == 0)
                {
                    //comment 1
                    builder.Append(""a"");//comment 2
                }//comment 4
                myString = builder.ToString();".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixesAddAssignmentWhenThereAre2WhilesOnBlock()
        {
            var source = @"
                var myString = """";
                while (DateTime.Now.Second % 2 == 1)
                {
                    var a = 1;
                }
                while (DateTime.Now.Second % 2 == 0)
                {
                    myString += ""a"";
                }".WrapInCSharpMethod();
            var fixtest = @"
                var myString = """";
                while (DateTime.Now.Second % 2 == 1)
                {
                    var a = 1;
                }
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                while (DateTime.Now.Second % 2 == 0)
                {
                    builder.Append(""a"");
                }
                myString = builder.ToString();".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixesAddAssignmentWithoutClashingTheBuilderName()
        {
            var source = @"
                var builder = 1;
                var myString = """";
                while (DateTime.Now.Second % 2 == 0)
                {
                    myString += ""a"";
                }".WrapInCSharpMethod();
            var fixtest = @"
                var builder = 1;
                var myString = """";
                var builder1 = new System.Text.StringBuilder();
                builder1.Append(myString);
                while (DateTime.Now.Second % 2 == 0)
                {
                    builder1.Append(""a"");
                }
                myString = builder1.ToString();".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
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
                while (DateTime.Now.Second % 2 == 0)
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
                while (DateTime.Now.Second % 2 == 0)
                {
                    builder2.Append(""a"");
                }
                myString = builder2.ToString();
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task ForWithtStringConcatOnLocalVariableCreatesDiagnostic()
        {
            var source = @"
                var myString = """";
                for (;;)
                {
                    myString += """";
                }".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixesAddAssignmentInFor()
        {
            var source = @"
                var myString = """";
                for (;;)
                {
                    myString += ""a"";
                    break;
                }".WrapInCSharpMethod();
            var fixtest = @"
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                for (;;)
                {
                    builder.Append(""a"");
                    break;
                }
                myString = builder.ToString();".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task ForeachWithtStringConcatOnLocalVariableCreatesDiagnostic()
        {
            var source = @"
                var myString = """";
                foreach (var i in new [] {1,2,3})
                {
                    myString += """";
                }".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixesAddAssignmentInForeach()
        {
            var source = @"
                var myString = """";
                foreach (var i in new [] {1,2,3})
                {
                    myString += ""a"";
                }".WrapInCSharpMethod();
            var fixtest = @"
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                foreach (var i in new [] {1,2,3})
                {
                    builder.Append(""a"");
                }
                myString = builder.ToString();".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task DoWithtStringConcatOnLocalVariableCreatesDiagnostic()
        {
            var source = @"
                var myString = """";
                do
                {
                    myString += """";
                } while (DateTime.Now.Second % 2 == 0);".WrapInCSharpMethod();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixesAddAssignmentInDo()
        {
            var source = @"
                var myString = """";
                do
                {
                    myString += ""a"";
                } while (DateTime.Now.Second % 2 == 0);".WrapInCSharpMethod();
            var fixtest = @"
                var myString = """";
                var builder = new System.Text.StringBuilder();
                builder.Append(myString);
                do
                {
                    builder.Append(""a"");
                } while (DateTime.Now.Second % 2 == 0);
                myString = builder.ToString();".WrapInCSharpMethod();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhileWithStringConcatOnFieldWithAccessCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class SomeObject
        {
			public string A = "";
        }
        class TypeName
        {
            private SomeObject someObject = new SomeObject();
            public void Foo()
            {
                while (DateTime.Now.Second % 2 == 0)
                {
                    someObject.A += """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "someObject.A"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhileWithStringConcatOnFieldWithAccessOnArrayCreatesDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class SomeObject
        {
			public string[] A;
        }
        class TypeName
        {
            private SomeObject someObject = new SomeObject();
            public void Foo()
            {
                while (DateTime.Now.Second % 2 == 0)
                {
                    someObject.A[DateTime.Now.Second] += """";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
                Message = string.Format(StringBuilderInLoopAnalyzer.MessageFormat, "someObject.A[DateTime.Now.Second]"),
                Severity = DiagnosticSeverity.Warning,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 21) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ForWithAssignmentOnIntArrayDoesNotCreateDiagnostic()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
				var length = 12;
				var baseOffset = 4;
				var branches = new int[length];
				for (int i = 0; i < length; i++)
					branches[i] = baseOffset + int.Parse("""");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WhileWithoutStringConcatWithMethoParameterDoesNotCreateDiagnostic()
        {
            var source = @"
public void Looper(int a = 0)
{
    while (a < 10)
    {
        a += 1;
    }
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}