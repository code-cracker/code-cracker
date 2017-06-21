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

        [Fact]
        public async Task WhenUsingIfAndElseWithAssignmentOfMethodResultChangeToTernaryFixGetsArgsApplied()
        {
            var source = @"
            int Method(int a) => a;

            public void Foo()
            {
                var something = true;
                int a;
                if (something)
                {
                    a = Method(1);
                }
                else
                {
                    a = Method(2);
                }
            }".WrapInCSharpClass();
            var fixtest = @"
            int Method(int a) => a;

            public void Foo()
            {
                var something = true;
                int a;
                a = Method(something?1:2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithAssignmentOfMethodResultWithComplexArgumentEvaluationChangeToTernaryFixGetsArgsApplied()
        {
            var source = @"
            int Method(int a) => a;

            public void Foo()
            {
                var something = true;
                int a;
                if (something)
                {
                    a = Method(1);
                }
                else
                {
                    a = Method(2 + 2);
                }
            }".WrapInCSharpClass();
            var fixtest = @"
            int Method(int a) => a;

            public void Foo()
            {
                var something = true;
                int a;
                a = Method(something?1:2 + 2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenUsingIfAndElseWithAssignmentToAnInterfaceVariableAFittingCastIsInserted()
        {
            var source = @"
            System.Collections.Generic.IEnumerable<int> e= null;
            if (true)
                e = new int[10];
            else
                e = new System.Collections.Generic.List<int>();
            ".WrapInCSharpMethod();
            var fixtest = @"
            System.Collections.Generic.IEnumerable<int> e= null;
            e = true ? (System.Collections.Generic.IEnumerable<int>)new int[10] : new System.Collections.Generic.List<int>();
            ".WrapInCSharpMethod();
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
        public async Task FixWhenReturningWithMethodWithSingleDifferentArgumentGetsArgsApplied()
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
        public async Task FixWhenReturningWithMethodWithMultipleArgumentsWhereSingleDifferentGetsArgsApplied()
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
        public async Task FixWhenReturningWithMethodWithMultipleArgumentsWhereMultipleDifferentGetsArgsNotApplied()
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
        public async Task FixWhenReturningWithMethodArgumentsGetCastedWhenGetsArgsApplied()
        {
            var source = @"
            class Base { }
            class A : Base { }
            class B : Base { }

            private int Method(Base b, string t) => 1;

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

            private int Method(Base b, string t) => 1;

            public int Foo()
            {
                return Method(true?(Base)new A():new B(),""hello"");
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithPrefixedMethodGetsArgsApplied()
        {
            var source = @"
            private int Method(int a) => a;

            public int Foo()
            {
                if (true)
                    return this.Method(1);
                else
                    return this.Method(2);
            }".WrapInCSharpClass();
            var fixtest = @"
            private int Method(int a) => a;

            public int Foo()
            {
                return this.Method(true?1:2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodOfPropertyGetsArgsApplied()
        {
            var source = @"
            class A {
                private int Method(int a) => a;
            }

            public int Foo()
            {
                var a=new A();
                if (true)
                    return a.Method(1);
                else
                    return a.Method(2);
            }".WrapInCSharpClass();
            var fixtest = @"
            class A {
                private int Method(int a) => a;
            }

            public int Foo()
            {
                var a=new A();
                return a.Method(true?1:2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodOfDifferentPropertyGetsArgsNotApplied()
        {
            var source = @"
            class A {
                public int Method(int a) => a;
            }
            A Prop1 { get { return new A(); } }
            A Prop2 { get { return new A(); } }

            public int Foo()
            {
                if (true)
                    return this.Prop1.Method(1);
                else
                    return this.Prop2.Method(2);
            }".WrapInCSharpClass();
            var fixtest = @"
            class A {
                public int Method(int a) => a;
            }
            A Prop1 { get { return new A(); } }
            A Prop2 { get { return new A(); } }

            public int Foo()
            {
                return true?this.Prop1.Method(1):this.Prop2.Method(2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodOfSameOverloadGetsArgsApplied()
        {
            var source = @"
            int Method(int a)=>a;
            int Method(string a)=>1;

            public int Foo()
            {                
                if (true)
                    return Method(1);
                else
                    return Method(2);
            }".WrapInCSharpClass();
            var fixtest = @"
            int Method(int a)=>a;
            int Method(string a)=>1;

            public int Foo()
            {                
                return Method(true?1:2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodOfDifferentOverloadGetsArgsNotApplied()
        {
            var source = @"
            int Method(int a)=>a;
            int Method(string a)=>1;

            public int Foo()
            {                
                if (true)
                    return Method(1);
                else
                    return Method(""2"");
            }".WrapInCSharpClass();
            var fixtest = @"
            int Method(int a)=>a;
            int Method(string a)=>1;

            public int Foo()
            {                
                return true?Method(1):Method(""2"");
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodOfDifferentOverloadButCastingPossibleGetsArgsNotApplied()
        {
            var source = @"
            public class A { }
            public class B:A { }
            void Method(A a) { };
            void Method(B b) { };

            public int Foo()
            {                
                if (true)
                    return Method(new A());
                else
                    return Method(new B());
            }".WrapInCSharpClass();
            var fixtest = @"
            public class A { }
            public class B:A { }
            void Method(A a) { };
            void Method(B b) { };

            public int Foo()
            {                
                return true?Method(new A()):Method(new B());
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodNestedInMemberAccessGetsArgsNotApplied()
        {
            var source = @"
            class A {
                public int Prop { get; }
            }
            
            A GetA(int i) => new A();

            public int Foo()
            {
                if (true)
                    return GetA(1).Prop;
                else
                    return GetA(2).Prop;
            }".WrapInCSharpClass();
            var fixtest = @"
            class A {
                public int Prop { get; }
            }
            
            A GetA(int i) => new A();

            public int Foo()
            {
                return true?GetA(1).Prop:GetA(2).Prop;
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodParamOverloadAndNumerOfArgsAreEqualGetsApplied()
        {
            var source = @"
            private int M(params int[] args) { }

            public int Foo()
            {
                if (true)
                    return M(1,1);
                else
                    return M(1,2);
            }".WrapInCSharpClass();
            var fixtest = @"
            private int M(params int[] args) { }

            public int Foo()
            {
                return M(1,true?1:2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodParamOverloadAndNumerOfArgsAreDifferentGetsNotApplied()
        {
            var source = @"
            private int M(params int[] args) { }

            public int Foo()
            {
                if (true)
                    return M(1,1);
                else
                    return M(1,2,3);
            }".WrapInCSharpClass();
            var fixtest = @"
            private int M(params int[] args) { }

            public int Foo()
            {
                return true?M(1,1):M(1,2,3);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodOfDynamicObjGetsArgsNotApplied()
        {
            // Calls on dynamic objects get dispatched during runtime.
            // Therefore the semantic would be changed if we apply to arguments 
            // and casting is involved:
            // d.M(new A()) else d.M(new B()) -> d.M(cond?(Base)new A():new B());
            // is not the same as cond?d.M(new A()): d.M(new B()) on dynamic objects.
            var source = @"
            public class Base {}
            public class A: Base {}
            public class B: Base {}
            
            public int Foo()
            {
                dynamic d = new object();
                if (true)
                    return d.M(new A());
                else
                    return d.M(new B());
            }".WrapInCSharpClass();
            var fixtest = @"
            public class Base {}
            public class A: Base {}
            public class B: Base {}
            
            public int Foo()
            {
                dynamic d = new object();
                return true?d.M(new A()):d.M(new B());
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithMethodOfDynamicObjGetsArgsNeverApplied()
        {
            // arguments on dynamic method calls are never applied even if it would be save.
            // see comments above for why dynamic is dangerous.
            var source = @"
            public int Foo()
            {
                dynamic d = new object();
                if (true)
                    return d.M(1,1);
                else
                    return d.M(1,2);
            }".WrapInCSharpClass();
            var fixtest = @"
            public int Foo()
            {
                dynamic d = new object();
                return true?d.M(1,1):d.M(1,2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithConstructorGetsArgsApplied()
        {
            var source = @"
            public System.Collections.Generic.List<int> Foo()
            {                
                if (true)
                    return new System.Collections.Generic.List<int>(1);
                else
                    return new System.Collections.Generic.List<int>(2);
            }".WrapInCSharpClass();
            var fixtest = @"
            public System.Collections.Generic.List<int> Foo()
            {                
                return new System.Collections.Generic.List<int>(true?1:2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithConstructorWithIdenticalInitializerGetsArgsApplied()
        {
            var source = @"
            public new System.Collections.Generic.List<int> Foo()
            {                
                if (true)
                    return new System.Collections.Generic.List<int>(1) { 1 };
                else
                    return new System.Collections.Generic.List<int>(2) { 1 };
            }".WrapInCSharpClass();
            var fixtest = @"
            public new System.Collections.Generic.List<int> Foo()
            {                
                return new System.Collections.Generic.List<int>(true?1:2) { 1 };
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithConstructorWithDifferentInitializerGetsArgsNotApplied()
        {
            var source = @"
            public System.Collections.Generic.List<int> Foo()
            {                
                if (true)
                    return new System.Collections.Generic.List<int>(1) { 1 };
                else
                    return new System.Collections.Generic.List<int>(2) { 1, 2 };
            }".WrapInCSharpClass();
            var fixtest = @"
            public System.Collections.Generic.List<int> Foo()
            {                
                return true?new System.Collections.Generic.List<int>(1) { 1 } : new System.Collections.Generic.List<int>(2) { 1, 2 };
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithConstructorWithDifferentOverloadsGetArgsNotApplied()
        {
            var source = @"
            public class A
            {
                public A(int i) { }
                public A(string s) { }
            }
            public A Foo()
            {                
                if (true)
                    return new A(1);
                else
                    return new A(""1"");
            }".WrapInCSharpClass();
            var fixtest = @"
            public class A
            {
                public A(int i) { }
                public A(string s) { }
            }
            public A Foo()
            {                
                return true?new A(1):new A(""1"");
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithConstructorOfDifferentObjectsGetArgsNotApplied()
        {
            var source = @"
            public class A 
            {
                public A(int i) { } 
            }
            public class B
            {
                public B(int i) { } 
            }

            public Object Foo()
            {                
                if (true)
                    return new A(1);
                else
                    return new B(2);
            }".WrapInCSharpClass();
            var fixtest = @"
            public class A 
            {
                public A(int i) { } 
            }
            public class B
            {
                public B(int i) { } 
            }

            public Object Foo()
            {                
                return true?(object)new A(1):new B(2);
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task WhenReturnTypeIsAnInterfaceAFittingCastIsInserted()
        {
            var source = @"
            IComparable GetComparable()
            {
                if (true)
                    return 1;
                else
                    return ""1"";
            }".WrapInCSharpClass();
            var fixtest = @"
            IComparable GetComparable()
            {
                return true?(IComparable)1 : ""1"";
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenReturningWithReturnTypeIsExplicitConvertable()
        {
            var source = @"
            double GetNumber()
            {
                if (true)
                    return 1;
                else
                    return 1.1;
            }".WrapInCSharpClass();
            var fixtest = @"
            double GetNumber()
            {
                return true?(double)1:1.1;
            }".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }
    }
}