using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class UnnecessaryToStringInStringConcatenationTests : CodeFixVerifier<UnnecessaryToStringInStringConcatenationAnalyzer, UnnecessaryToStringInStringConcatenationCodeFixProvider>
    {
        private static DiagnosticResult CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(int expectedRow, int expectedColumn)
        {
            return new DiagnosticResult(DiagnosticId.UnnecessaryToStringInStringConcatenation.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(expectedRow, expectedColumn)
                .WithMessage("Unnecessary '.ToString()' call in string concatenation.");
        }

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
            var source = @"var foo = ""a"" + new System.Text.StringBuilder().ToString();".WrapInCSharpMethod();

            var expected = CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(10, 60);

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
            public override string ToString()
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
                var far = ""a"" + new AuxClass().ToString() + auxClass.ToString() + new int().ToString(""C"");
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
        public async Task CallToStringOnStringExpressionsShouldGenerateDiagnosticResult()
        {
            const string test = @"var t1 = (true ? ""1"" : ""2"") + new object().ToString();";

            var expected = CreateUnnecessaryToStringInStringConcatenationDiagnosticResult(1, 43);

            await VerifyCSharpDiagnosticAsync(test, expected);
        }

        [Fact]
        public async Task CallToStringFollowedByACallToAStringMethodShouldNotGenerateDiagnosticResult()
        {
            const string source = @"var salary = ""salary: "" + 1000.ToString().Trim();";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task CallToLambdaNamedToStringShouldNotGenerateDiagnosticResult()
        {
            var source = @"
            Func<string> ToString = () => ""Dummy"";
            var t = 1 + ToString();
            ".WrapInCSharpMethod();

            await VerifyCSharpHasNoDiagnosticsAsync(source);
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

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfOtherOverloadsTakePrecedence_NumericAddition_RightSide()
        {
            const string source = @"var value = 1 + 2.ToString();";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfOtherOverloadsTakePrecedence_NumericAddition_LeftSide()
        {
            const string source = @"var value = 2.ToString() + 1;";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfOtherOverloadsTakePrecedence_NumericAddition_WithExpression()
        {
            const string source = @"var value = (1 + 1) + 2.ToString();";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfOtherOverloadsTakePrecedence_NumericAddition_Double()
        {
            const string source = @"var value = (true ? 1.1 : 0.99) + 2.ToString();";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfOtherOverloadsTakePrecedence_NumericAddition_DateTime()
        {
            const string source = @"var value = new System.DateTime(2000, 1, 1) + 2.ToString();";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfOtherOverloadsTakePrecedence_CompilerGeneratedEnumOperator()
        {
            const string source = @"var value = System.AttributeTargets.Assembly + System.AttributeTargets.Module.ToString();";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfOtherOverloadsTakePrecedence_UnderlyingTypeDoesntHaveAddOperatorOverload()
        {
            const string source = @"var value = new System.Random() + new System.Random().ToString();";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfOtherOverloadsTakePrecedence_UserDefinedOperator()
        {
            const string source = @"
    namespace A
    {
        public class C1
        {
            public static string operator +(C1 c, object o) => ""Dummy"";
        }

        public class C2
        {
            public void M()
            {
                var t = new C1().ToString() + ""a"";
            }
        }
    }
";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfOtherOverloadsTakePrecedence_DelegateCombination()
        {
            var source = @"
            var ea1 = new System.EventHandler((o, e) => { });
            var ea2 = new System.EventHandler((o, e) => { });
            var t = ea1 + ea2.ToString();
            ".WrapInCSharpMethod();

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfTheTypesOfTheOperationAreNotResovable_ToStringReceiver()
        {
            const string source = @"var t = new UndefinedType().ToString() + ""a""";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfTheTypesOfTheOperationAreNotResovable_OtherSide()
        {
            const string source = @"var t = 1.ToString() + new UndefinedType();";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task AStringConcatinationShouldNotBeRemovedIfTheTypesOfTheOperationAreNotResovable_SyntaxError()
        {
            const string source = @"var t = new System.Random().ToString() + new ThisIsAnSyntaxError";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FixReplacesToStringCallInAStringConcatenationWithAVariable()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var text = ""def"";
                var a = ""abc"" + text.ToString();
                Console.Log(a);
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var text = ""def"";
                var a = ""abc"" + text;
                Console.Log(a);
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }


        [Fact]
        public async Task FixReplacesToStringCallInAStringConcatenation()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var a = ""abc"" + ""def"".ToString();
                Console.Log(a);
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var a = ""abc"" + ""def"";
                Console.Log(a);
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async Task FixReplacesToStringCallInAStringConcatenationWithAnObject()
        {
            const string test = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var foo = ""a"" + new object().ToString();
                Console.Log(foo);
            }
        }
    }";

            const string expected = @"
    using System;

    namespace ConsoleApplication1
    {
        class TypeName
        {
            public async Task Foo()
            {
                var foo = ""a"" + new object();
                Console.Log(foo);
            }
        }
    }";
            await VerifyCSharpFixAsync(test, expected);
        }
    }
}