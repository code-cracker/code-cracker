using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;
using System;
using Microsoft.CodeAnalysis.Testing;

namespace CodeCracker.Test.CSharp.Style
{
    public class ForInArrayTests : CodeFixVerifier<ForInArrayAnalyzer, ForInArrayCodeFixProvider>
    {
        [Fact]
        public async Task ForWhenAccessingAnotherArrayDoesNotCreateDiagnostic()
        {
            var source = @"
var array = new [] {1};
var anotherArray = new [] {1};
var count = 0;
var anotherCount = 0;
for (var i = 0; i < array.Length; i++)
{
    var item = array[i];
    count += array[i];
    anotherCount += anotherArray[i];
}".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithEmptyDeclarationAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                var i = 0;
                for ( ; i < array.Length; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithEmptyConditionAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; ; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithEmptyIncrementorsAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; )
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForAsWhileTrueAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var i = 1;
                var array = new [] {1};
                for ( ; ; )
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithMultipleDeclarationsAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0, j = 1; i < array.Length; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithMultipleIncrementorsAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                var j = 0;
                for (var i = 0; i < array.Length; i++, j++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithSingleBodyAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                    Console.WriteLine(1);
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithBodyThatDoesNotAssignCurrentIndexToAVariableFromArrayAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithBodyThatDoesNotUseLessThanAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i >= array.Length; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithBodyThatDoesAssignesAnotherVariableToAVariableFromArrayAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                var j = 1;
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[j];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithBodyThatAssignesCurrentIndexToAVariableFromAnotherArrayAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                var anotherArray = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                    var item = anotherArray[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithConditionalThatDoesNoIterateFullArrayAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length - 2; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithDeclarationNotStartingOnZeroAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 1; i < array.Length; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWhereIndexIsUsedThroughtBodyAnalyzerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[i];
                    var j = i;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithoutABinaryConditionDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var array = new int[] { 0 };
                for (var i = 0; Bar; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForWithDeclarationInitializedWithAnotherVariableDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public void Foo()
            {
                var array = new int[] { 0 };
                var start = 0;
                for (int i = start; i < array.Length; i++)
                {
                    var item = array[i];
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ForInArrayAnalyzerCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[i];
                    // whatever comes after
                }
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ForInArray.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(9, 17)
                .WithMessage("You can use foreach instead of for.");
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ForInArrayAnalyzerWhereArrayIsInitializedOutsideTheScopeCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Bar()
            {
                var array = new [] {1};
                Foo(array);
            }
            public int Foo(int[] array)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[i];
                    // whatever comes after
                }
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ForInArray.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(13, 17)
                .WithMessage("You can use foreach instead of for.");
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ForInArrayAnalyzerWhenHasMultipleElementAccessCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Bar()
            {
                var array = new [] {1};
                var count = 0;
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[i];
                    count += array[i];
                    Console.WriteLine(array[i]);
                }
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ForInArray.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(10, 17)
                .WithMessage("You can use foreach instead of for.");
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WhenUsingForWithAnArrayThenChangesToForeach()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Foo()
            {
                var array = new [] {1};
                for (var i = 0; i < array.Length; i++)
                {
                    string a;
                    // whatever comes before
                    var item = array[i];
                    // whatever comes after
                    string b;
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
                var array = new [] {1};
                foreach (var item in array)
                {
                    string a;
                    // whatever comes before
                    // whatever comes after
                    string b;
                }
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingForWithAnArrayWithMultipleElementAccessThenChangesToForeach()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Bar()
            {
                var array = new [] {1};
                var count = 0;
                for (var i = 0; i < array.Length; i++)
                {
                    var item = array[i];
                    count += array[i];
                    Console.WriteLine(array[i]);
                }
            }
        }
    }";

            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Bar()
            {
                var array = new [] {1};
                var count = 0;
                foreach (var item in array)
                {
                    count += item;
                    Console.WriteLine(item);
                }
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenUsingForWithAnArrayDeclaredOutsideTheScopeThenChangesToForeach()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Bar()
            {
                var array = new [] {1};
                Foo(array);
            }
            public int Foo(int[] array)
            {
                for (var i = 0; i < array.Length; i++)
                {
                    string a;
                    // whatever comes before
                    var item = array[i];
                    // whatever comes after
                    string b;
                }
            }
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int Bar()
            {
                var array = new [] {1};
                Foo(array);
            }
            public int Foo(int[] array)
            {
                foreach (var item in array)
                {
                    string a;
                    // whatever comes before
                    // whatever comes after
                    string b;
                }
            }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest, 0);
        }

        [Fact]
        public async Task WhenThereIsAnAssignmentDoesNotCreateDiagnostic()
        {
            const string source = @"
class Baz
{
    void Foo()
    {
        var buffer = new[] { 1 };
        for (int i = 0; i < buffer.Length; i++)
        {
            var temp = buffer[i];
            buffer[i] = 1;
        }
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreIfNotEnumerable()
        {
            const string source = @"
class Foo
{
    void Bar()
    {
        var list = new MyList();
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
        }
    }
}
class MyList
{
    public int Count => 1;
    public int this[int index] => 1;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IfHasMethodGetEnumeratorReturningIEnumeratorCreatesDiagnostic()
        {
            const string source = @"
class Foo
{
    void Bar()
    {
        var list = new MyList();
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
        }
    }
}
class MyList
{
    public int Count => 1;
    public int this[int index] => 1;
    public System.Collections.IEnumerator GetEnumerator()
    {
        yield return 1;
    }
}";
            var expected = new DiagnosticResult(DiagnosticId.ForInArray.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(7, 9)
                .WithMessage(ForInArrayAnalyzer.MessageFormat);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IfHasMethodGetEnumeratorReturningClassThatImplementsIEnumeratorCreatesDiagnostic()
        {
            const string source = @"
class Foo
{
    void Bar()
    {
        var list = new MyList();
        for (int i = 0; i < list.Count; i++)
        {
            var item = list[i];
        }
    }
}
public class MyEnumerator : System.Collections.IEnumerator
{
    public object Current => 1;
    public bool MoveNext() { }
    public void Reset() { }
}
class MyList
{
    public int Count => 1;
    public int this[int index] => 1;
    public MyEnumerator GetEnumerator()
    {
        yield return 1;
    }
}";
            var expected = new DiagnosticResult(DiagnosticId.ForInArray.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(7, 9)
                .WithMessage(ForInArrayAnalyzer.MessageFormat);
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WithFieldCreatesDiagnostic()
        {
            var source = @"
private int[] array = new [] {1};
public int Foo()
{
    for (var i = 0; i < array.Length; i++)
    {
        var item = array[i];
    }
}".WrapInCSharpClass();
            var expected = new DiagnosticResult(DiagnosticId.ForInArray.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(12, 5)
                .WithMessage("You can use foreach instead of for.");
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WithPropertyCreatesDiagnostic()
        {
            var source = @"
public int[] Foo
{
    set
    {
        for (var i = 0; i < value.Length; i++)
        {
            var item = value[i];
        }
    }
}
}".WrapInCSharpClass();
            var expected = new DiagnosticResult(DiagnosticId.ForInArray.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(13, 9)
                .WithMessage("You can use foreach instead of for.");
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task WithValueTypeNoDiagnostic()
        {
            const string source = @"
            using System;
            using System.Collections.Generic;

            namespace Test
            {
                struct Foo
                {
                    public int X;
                }

                public class bar
                {
                    static void Goo()
                    {
                        var array = new Foo[1];
                        for (int i = 0; i < array.Length; i++)
                        {
                            var actual = array[i];
                            array[i].X = 5;
                        }
                    }
                }
            }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WithValueTypeAndIEnumeratorDoesNotCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    void Bar()
    {
        var list = new MyList();
        for (int i = 0; i < list.Count; i++)
        {
            var actual = list[i];
            actual.X = 5;
        }
    }
}
struct Struct
{
    public int X { get; set; }
}
class MyList
{
    public int Count => 1;
    public Struct this[int index] => new Struct();
    public System.Collections.Generic.IEnumerator<Struct> GetEnumerator()
    {
        yield return new Struct();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task WithValueTypeListNoDiagnostic()
        {
            const string source = @"
            using System;
            using System.Collections.Generic;

            namespace Test
            {
                struct Foo
                {
                    public int X;
                }

                public class bar
                {
                    static void Goo()
                    {
                        var array = new List<Foo>();
                        for (int i = 0; i < array.Length; i++)
                        {
                            var actual = array[i];
                            array[i].X = 5;
                        }
                    }
                }
            }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenWouldChangeTheIterationVariableWithAssignment()
        {
            var source = @"
var a = new[] { 1 };
for (var i = 0; i < a.Length; i++)
{
    var item = a[i];
    item = 0;
}".WrapInCSharpMethod();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenWouldChangeTheIterationVariableWithRef()
        {
            var source = @"
void Foo()
{
    var a = new[] { 1 };
    for (var i = 0; i < a.Length; i++)
    {
        var item = a[i];
        Bar(ref item);
    }
}
void Bar(ref int i)
{
    i = 1;
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenWouldChangeTheIterationVariableWithPostfixUnary()
        {
            var source = @"
void Foo()
{
    var a = new[] { 1 };
    for (var i = 0; i < a.Length; i++)
    {
        var item = a[i];
        item++;
    }
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoreWhenWouldChangeTheIterationVariableWithPrefixUnary()
        {
            var source = @"
void Foo()
{
    var a = new[] { 1 };
    for (var i = 0; i < a.Length; i++)
    {
        var item = a[i];
        ++item;
    }
}".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}