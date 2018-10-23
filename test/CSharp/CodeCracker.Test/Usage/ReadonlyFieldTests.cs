using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class ReadonlyFieldTests : CodeFixVerifier<ReadonlyFieldAnalyzer, ReadonlyFieldCodeFixProvider>
    {
        [Fact]
        public async Task IgnorePreIncrement()
        {
            const string source = @"
class TypeName
{
    static int counter = 1;
    static void Main() => ++counter;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task IgnoreOut()
        {
            const string source = @"
public class C
{
    private string field = "";
    private static void Foo(out string bar) => bar = "";
    public void Baz() => Foo(out field);
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task IgnoreRef()
        {
            const string source = @"
public class C
{
    private string field = "";
    private static void Foo(ref string bar) => bar = "";
    public void Baz() => Foo(ref field);
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task IgnorePostIncrement()
        {
            const string source = @"
class TypeName
{
    static int counter = 1;
    static void Main() => counter++;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task IgnorePreDecrement()
        {
            const string source = @"
class TypeName
{
    static int counter = 1;
    static void Main() => --counter;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task IgnorePostDecrement()
        {
            const string source = @"
class TypeName
{
    static int counter = 1;
    static void Main() => counter--;
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task IgnoreAssignmentToFieldsInOtherTypes()
        {
            const string source1 = @"
class TypeName1
{
    public int i;
}";
            const string source2 = @"
class TypeName2
{
    public TypeName2()
    {
        var t = new TypeName1();
        t.i = 1;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source1, source2 });
        }

        [Fact]
        public async Task FieldWithoutAssignmentDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithoutAssignmentInAStructDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        struct TypeName
        {
            private int i;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task PublicFieldWithAssignmentOnDeclarationDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public int i = 1;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentOnDeclarationAlreadyReadonlyDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int i = 1;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ConstantFieldDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private const int i = 1;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldWithoutModifierWithAssignmentOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            int i = 1;
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 17)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldsWithAssignmentOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            private int j = 1;
        }
    }";
            var expected1 = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            var expected2 = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(7, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "j"));
            await VerifyCSharpDiagnosticAsync(source, expected1, expected2);
        }

        [Fact]
        public async Task TwoClassesWithFieldsWithAssignmentOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName1
        {
            private int i = 1;
            private int j = 1;
        }
        class TypeName2
        {
            private int k = 1;
            private int l = 1;
        }
    }";
            var expected1 = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            var expected2 = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(7, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "j"));
            var expected3 = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(11, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "k"));
            var expected4 = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(12, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "l"));
            await VerifyCSharpDiagnosticAsync(source, new[] { expected1, expected2, expected3, expected4 });
        }

        [Fact]
        public async Task FieldWithAssignmentOnDeclarationAndNestedNamespaceCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        namespace SubNamespace
        {
            class TypeName
            {
                private int i = 1;
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(8, 29)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldWithtAssignmentOnDeclarationInAStructCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        struct TypeName
        {
            private int i = 1;
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldWithAddAssignmentDoesNotCreateDiagnostic()
        {
            const string source = @"
class TypeName
{
    private int i = 0;
    public void Foo()
    {
        i += 0;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 0;
            public void Foo()
            {
                i = 0;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentInAStructDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        struct TypeName
        {
            private int i = 0;
            public void Foo()
            {
                i = 0;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldWithAssignmentOnConstructorCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
            TypeName()
            {
                i = 0;
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FieldWithAssignmentOnConstructorAndOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            TypeName()
            {
                i = 0;
            }
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 25)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ChangeFieldAssignedOnDeclarationToReadonly()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int i = 1;
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task ChangeFieldAssignedOnDeclarationWithoutModifierToReadonly()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            int i = 1;
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            readonly int i = 1;
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FieldsWithAssignmentOnDeclarationWithSingleDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i, j = 1;
        }
    }";
            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 28)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "j"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ChangeFieldWhenOtherFieldsAreDeclared()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
            private int j = 1;
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i;
            private readonly int j = 1;
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task ChangeFieldWithJointDeclarationToADifferentDeclaration()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            //leading
            private int i, j = 1;//trailling
            //leading trivia for token
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            //leading
            private int i;//trailling
            private readonly int j = 1;
            //leading trivia for token
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task ChangeFieldWithJointDeclarationToADifferentDeclarationWhenThereIsNoPrivateModifier()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            //leading
            int i, j = 1;//trailling
            //leading trivia for token
        }
    }";
            const string fixtest = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            //leading
            int i;//trailling
            readonly int j = 1;
            //leading trivia for token
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FieldWithAssignmentOnConstructorThatIsAlreadyReadonlyDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private readonly int i;
            public TypeName()
            {
                i = 0;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnPropertyGetDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            public int Property
            {
                get
                {
                    i = 0;
                    return 1;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnPropertySetDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            public int Property
            {
                set
                {
                    i = 0;
                }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnEventAddDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            public event System.EventHandler MyEvent
            {
                add { i = 0; }
                remove { }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnEventRemoveDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            public event System.EventHandler MyEvent
            {
                add { }
                remove { i = 0; }
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldAssignedOnDelegateDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private int i = 1;
            void Foo()
            {
                System.Action a = () => { i = 0; };
            }
        }
   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task StaticFieldUpdatedInInstanceConstructorDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int i;
            TypeName()
            {
                i = 1;
            }
        }
   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task StaticFieldInitializedOnDeclarationUpdatedInInstanceConstructorDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int i = 0;
            TypeName()
            {
                i = 1;
            }
        }
   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task StaticFieldInitializedOnStaticConstructorCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int i;
            static TypeName()
            {
                i = 1;
            }
        }
   }";
            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 32)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task StaticFieldInitializedOnDeclarationCreatesDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class TypeName
        {
            private static int i = 1;
        }
   }";
            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 32)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "i"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task UpdatesOnInnerClassesDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class OuterClass
        {
            private int i = 0;
            class InnerClass
            {
                OuterClass o;
                void Foo()
                {
                    o.i = 1;
                }
            }
        }
   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldsWithoutAssignmentOnPartialClassOn2FilesDoesNotCreateDiagnostic()
        {
            const string source1 = @"
    namespace ConsoleApplication1
    {
        partial class TypeName
        {
            private int i;
            public void Foo2()
            {
                j = 0;
            }
        }
    }";
            const string source2 = @"
    namespace ConsoleApplication1
    {
        partial class TypeName
        {
            private int j;
            public void Foo()
            {
                i = 0;
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source1, source2);
        }

        [Fact]
        public async Task FieldsAssignedOnLambdaDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        internal class Test
        {
            private string _value;

            public Test()
            {
                Func<string> factory = () => _value ?? (_value = ""Hello"");
            }
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FieldsAssignedOnLambdaWithInitializerDoesNotCreateDiagnostic()
        {
            const string source = @"
using System;
class C
{
    private readonly Action set;
    private int i = 0;

    public C()
    {
        set = () => i = 1;
    }

    public void Modify()
    {
        set();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task VariableInitializerDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        class A
        {
            public int X;

            public A()
            {
                X = 5;
            }
        }

        static void B()
        {
            var c = new A { X = 7 };
        }
   }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task UserDefinedStructFieldDoesNotCreateDiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class MyClass
        {
            private MyStruct myStruct = default(MyStruct);

            private struct MyStruct
            {
                public int Value;
            }
        }
    }
    ";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DateTimeFieldInitializedOnDeclarationDoesNotCreateDiagnostic()
        {
            const string source = @"
    using System;

    namespace ConsoleApplication1
    {
        public class MyClass
        {
            private DateTime date = new DateTime(2008, 5, 1, 8, 30, 52);
        }
    }
    ";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task EnumerationFieldInitializedOnDeclarationCreatesADiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class MyClass
        {
            private VehicleType car = VehicleType.Car;

            private enum VehicleType
            {
                None = 0,
                Car = 1,
                Truck = 2
            }
        }
    }
    ";

            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 33)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "car"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task ReferenceTypeFieldInitializedInConstructorCreatesADiagnostic()
        {
            const string source = @"
    namespace ConsoleApplication1
    {
        public class Person
        {
            private string name;

            public Person(string name)
            {
                this.name = name;
            }
        }
    }
    ";

            var expected = new DiagnosticResult(DiagnosticId.ReadonlyField.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(6, 28)
                .WithMessage(string.Format(ReadonlyFieldAnalyzer.Message, "name"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IgnoreWhenConstructorIsTheLastMember()
        {
            const string source = @"
class Test
{
    private int value;
    public int Value
    {
        get { return value; }
        set { this.value = value; }
    }
    public void Foo()
    {
        value = 1;
    }
    public Test()
    {
        value = 8;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ComplexTypeDoesNotCreateDiagnosticAsync()
        {
            const string source = @"
class C
{
    private S s;

    public C()
    {
        s = default(S);
    }

    public void M1()
    {
        s.Value = 1;
    }

    public struct S
    {
        public int Value;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}