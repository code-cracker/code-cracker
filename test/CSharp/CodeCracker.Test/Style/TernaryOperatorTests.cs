using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class TernaryOperatorWithAssignmentTests : CodeFixVerifier<TernaryOperatorAnalyzer, TernaryOperatorWithAssignmentCodeFixProvider>
    {
        [Fact]
        public async Task WhenUsingIfWithoutElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                    return 1;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task TwoIfsInARowAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                string s;
                if (s == ""A"")
                {
                    DoSomething();
                }
                else if (s == ""A"")
                {
                    DoSomething();
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithAssignmentChangeToTernaryFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                if (something)
                {
                    a = ""a"";
                }
                else
                {
                    a = ""b"";
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                a = something ? ""a"" : ""b"";
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithNullableValueTypeAssignmentChangeToTernaryFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var something = true;
                int? a;
                if (something)
                {
                    a = 1;
                }
                else
                {
                    a = null;
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
                var something = true;
                int? a;
                a = something ? 1 : (int?)null;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithAssignmentChangeToTernaryFixAll()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                if (something)
                {
                    a = ""a"";
                }
                else
                {
                    a = ""b"";
                }
                if (something)
                {
                    a = ""a"";
                }
                else
                {
                    a = ""b"";
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                a = something ? ""a"" : ""b"";
                a = something ? ""a"" : ""b"";
            }
        }
    }";
            await VerifyCSharpFixAllAsync(new string[] { source, source.Replace("TypeName", "TypeName1") }, new string[] { fixtest, fixtest.Replace("TypeName", "TypeName1") });
        }


        [Fact]
        public async Task WhenUsingIfAndElseWithComplexAssignmentChangeToTernaryFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                if (something)
                {
                    a = ""a"" + ""b"";
                }
                else
                {
                    a = ""c"" + GetInfo(1);
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                a = something ? ""a"" + ""b"" : ""c"" + GetInfo(1);
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithComplexAssignmentChangeToTernaryFixAll()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                if (something)
                {
                    a = ""a"" + ""b"";
                }
                else
                {
                    a = ""c"" + GetInfo(1);
                }
                if (something)
                {
                    a = ""a"" + ""b"";
                }
                else
                {
                    a = ""c"" + GetInfo(1);
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                a = something ? ""a"" + ""b"" : ""c"" + GetInfo(1);
                a = something ? ""a"" + ""b"" : ""c"" + GetInfo(1);
            }
        }
    }";
            await VerifyCSharpFixAllAsync(new string[] { source, source.Replace("TypeName", "TypeName1") }, new string[] { fixtest, fixtest.Replace("TypeName", "TypeName1") });
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithAssignmentAnalyzerCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                string a;
                if (something)
                {
                    a = ""a"";
                }
                else
                {
                    a = ""b"";
                }
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId(),
                Message = "You can use a ternary operator.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 17) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixConsidersAssignmentType()
        {
            const string source = @"
class Base { }
class A : Base { }
class B : Base { }
class Test2
{
    public static void Foo()
    {
        var something = true;
        Base b;
        if (something)
            b = new A();
        else
            b = new B();
    }
}
";
            const string fixtest = @"
class Base { }
class A : Base { }
class B : Base { }
class Test2
{
    public static void Foo()
    {
        var something = true;
        Base b;
        b = something ? (Base)new A() : new B();
    }
}
";
            await VerifyCSharpFixAsync(source, fixtest);
        }
    }

    public class TernaryOperatorWithReturnTests : CodeFixVerifier<TernaryOperatorAnalyzer, TernaryOperatorWithReturnCodeFixProvider>
    {
        [Fact]
        public async Task WhenUsingIfAndElseWithDirectReturnChangeToTernaryFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                    return 1;
                else
                    return 2;
            }
        }
    }";

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                return something ? 1 : 2;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithNullableValueTypeDirectReturnChangeToTernaryFix()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int? Foo()
            {
                var something = true;
                if (something)
                    return 1;
                else
                    return null;
            }
        }
    }";

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int? Foo()
            {
                var something = true;
                return something ? 1 : (int?)null;
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, allowNewCompilerDiagnostics: true);
        }

        [Fact]
        public async Task FixWhenThereIsNumericImplicitConversion()
        {
            var source = @"
static double OnReturn()
{
    var condition = true;
    double aDouble = 2;
    var bInteger = 3;
    if (condition)
        return aDouble;
    else
        return bInteger;
}".WrapInCSharpClass();
            var fixtest = @"
static double OnReturn()
{
    var condition = true;
    double aDouble = 2;
    var bInteger = 3;
    return condition ? aDouble : bInteger;
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenThereIsImplicitConversionWithZeroAndEnum()
        {
            var source = @"
enum FooBar
{
    one, two
}
static FooBar OnReturn()
{
    var condition = true;
    var fooBar = FooBar.one;
    if (condition)
        return 0;
    else
        return fooBar;
}".WrapInCSharpClass();
            var fixtest = @"
enum FooBar
{
    one, two
}
static FooBar OnReturn()
{
    var condition = true;
    var fooBar = FooBar.one;
    return condition ? 0 : fooBar;
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenThereIsImplicitConversionWithEnumAndZero()
        {
            var source = @"
enum FooBar
{
    one, two
}
static FooBar OnReturn()
{
    var condition = true;
    var fooBar = FooBar.one;
    if (condition)
        return fooBar;
    else
        return 0;
}".WrapInCSharpClass();
            var fixtest = @"
enum FooBar
{
    one, two
}
static FooBar OnReturn()
{
    var condition = true;
    var fooBar = FooBar.one;
    return condition ? fooBar : 0;
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningAnObject()
        {
            var source = @"
static object OnReturn()
{
    var condition = true;
    var aInt = 2;
    var anObj = new object();
    if (condition)
        return anObj;
    else
        return aInt;
}".WrapInCSharpClass();
            var fixtest = @"
static object OnReturn()
{
    var condition = true;
    var aInt = 2;
    var anObj = new object();
    return condition ? anObj : aInt;
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithDirectReturnChangeToTernaryFixAll()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                    return 1;
                else
                    return 2;
                if (something)
                    return 1;
                else
                    return 2;
            }
        }
    }";

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                return something ? 1 : 2;
                return something ? 1 : 2;
            }
        }
    }";
            await VerifyCSharpFixAllAsync(new string[] { source, source.Replace("TypeName", "TypeName1") }, new string[] { fixtest, fixtest.Replace("TypeName", "TypeName1") });
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithoutReturnOnIfAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    string a = null;
                }
                else
                {
                    return 2;
                }
                return 1;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithoutReturnOnElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    return 2;
                }
                else
                {
                    string a = null;
                }
                return 1;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithBlockWith2StatementsOnElseAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    return 1;
                }
                else
                {
                    string a = null;
                    return 2;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfWithElseButWithBlockWith2StatementsOnIfAnalyzerDoesNotCreateDiagnostic()
        {
            const string sourceWithoutElse = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                {
                    string a = null;
                    return 1;
                }
                else
                {
                    return 2;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(sourceWithoutElse);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithDirectReturnAnalyzerCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var something = true;
                if (something)
                    return 1;
                else
                    return 2;
            }
        }
    }";
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.TernaryOperator_Return.ToDiagnosticId(),
                Message = "You can use a ternary operator.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 9, 17) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixConsidersReturnType()
        {
            const string source = @"
class Base { }
class A : Base { }
class B : Base { }
class Test
{
    public static Base Foo()
    {
        var something = true;
        if (something)
            return new A();
        else
            return new B();
    }
}
";
            const string fixtest = @"
class Base { }
class A : Base { }
class B : Base { }
class Test
{
    public static Base Foo()
    {
        var something = true;
        return something ? (Base)new A() : new B();
    }
}
";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenReturnStatementContainsMethodCallAnalyzerCreatesDiagnostic()
        {
            var source = @"
            private int Method(int i) => i;

            public int Foo()
            {
                if (true)
                    return Method(1);
                else
                    return Method(2);
            }".WrapInCSharpClass();
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.TernaryOperator_Return.ToDiagnosticId(),
                Message = "You can use a ternary operator.",
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 13, 17) }
            };
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodWithSingleDifferentArgumentGetsSimplified()
        {
            var source = @"
            private int Method(int i) => i;

            public int Foo()
            {
                if (true)
                    return Method(1);
                else
                    return Method(2);
            }".WrapInCSharpClass();
            var fixtest = @"
            private int Method(int i) => i;

            public int Foo()
            {
                return Method(true?1:2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodWithMultipleArgumentsWhereSingleDifferentGetsSimplified()
        {
            var source = @"
            private int Method(int i, string t) => i;

            public int Foo()
            {
                if (true)
                    return Method(1, ""hello"");
                else
                    return Method(2, ""hello"");
            }".WrapInCSharpClass();
            var fixtest = @"
            private int Method(int i, string t) => i;

            public int Foo()
            {
                return Method(true?1:2, ""hello"");
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodWithMultipleArgumentsWhereMultipleDifferentGetsNotSimplified()
        {
            var source = @"
            private int Method(int i, string t) => i;

            public int Foo()
            {
                if (true)
                    return Method(1, ""hello1"");
                else
                    return Method(2, ""hello2"");
            }".WrapInCSharpClass();
            var fixtest = @"
            private int Method(int i, string t) => i;

            public int Foo()
            {
                return true?Method(1,""hello1""):Method(2, ""hello2"");
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodArgumentsGetCastedWhenSimplified()
        {
            var source = @"
            class Base { }
            class A : Base { }
            class B : Base { }

            private string Method(Base b, string t) => t;

            public int Foo()
            {
                if (true)
                    return Method(new A(), ""hello"");
                else
                    return Method(new B(), ""hello"");
            }".WrapInCSharpClass();
            var fixtest = @"
            class Base { }
            class A : Base { }
            class B : Base { }

            private string Method(Base b, string t) => t;

            public int Foo()
            {
                return Method(true?(Base)new A():new B(),""hello"");
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}