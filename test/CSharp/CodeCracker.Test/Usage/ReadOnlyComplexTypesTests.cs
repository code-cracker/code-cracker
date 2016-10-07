using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class ReadOnlyComplexTypesTests : CodeFixVerifier<ReadOnlyComplexTypesAnalyzer, ReadonlyFieldCodeFixProvider>
    {
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
        public async Task dateTimeCreateDiagnostics()
        {
            const string source1 = @"
namespace codeCrackerConsole
{
    public class MyClass
    {
        private readonly DateTime dt = new DateTime(1, 1, 2015);
    }
}";
            const string source2 = @"
namespace codeCrackerConsole
{
    public class MyClass
    {
        private readonly DateTime dt = new DateTime(1, 1, 2015);
    }
}";
            await VerifyCSharpFixAsync(source1, source2, 0);
        }
        [Fact]
        public async Task protectedFieldDontCreateDiagnostics() {
            const string source = @"
            namespace ConsoleApplication1
            {
public class MyClass
    {
        protected MyStruct myStruct = default(MyStruct);
        private struct MyStruct
        {
            public int Value;
        }
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task publicFieldDontCreateDiagnostics()
        {
            const string source = @"
            namespace ConsoleApplication1
            {
    public class MyClass
    {
        public  MyStruct myStruct = default(MyStruct);
        private struct MyStruct
        {
            public int Value;
        }
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task publicFieldWithClassDontCreateDiagnostics()
        {
            const string source = @"
            namespace ConsoleApplication1
            {
    public class MyClass
    {
        public  MyStruct myStruct = new MyStruct();
        private struct MyStruct
        {
            public int Value;
        }
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }      

        [Fact]
        public async Task readOnlyVarDontCreateDiagnostics()
        {
            const string source = @"
            namespace ConsoleApplication1
            {
     public class MyClass
    {
        readonly var s = "";
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task readOnlyFieldDontCreateDiagnostics()
        {
            const string source = @"
            namespace ConsoleApplication1
            {
     public class MyClass
    {
        readonly string s;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task primitiveTypesDontCreateDiagnostics()
        {
            const string source = @"
            namespace ConsoleApplication1
            {
    class test 
    {
        private byte b = new byte();
        private sbyte s = new sbyte();
        private int i = new int();
        private uint u = new uint();
        private short ss = new short();
        public ushort us = new ushort();
        public long l = new long();
        public ulong ul = new ulong();
        public float fl = new float();
        public double d = new double();
        public char c = new char();
        public bool bo = new bool();
        public object o = new object();
        public string st = "";
        public decimal dc = new decimal();                   
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source });
        }

        [Fact]
        public async Task enumDontCreateDiagnostics()
        {
            const string source = @"
            namespace ConsoleApplication1
            {
    public class MyClass
    {
        private test testEnum;
        public enum test
        {
            test1 = 1,
            test2 = 2
        }
    }
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
        public async Task structNoDiagnostic()
        {
            const string source1 = @"
            namespace ConsoleApplication1
            {
                public class MyClass
                {
                    private MyStruct myStruct;
                    private struct MyStruct
                    {
                        public int Value;
                    }
                }        
            }";
            await VerifyCSharpHasNoDiagnosticsAsync(new[] { source1 });
        }

        [Fact]
        public async Task structWithNullValue()
        {
            const string source1 = @"
            namespace ConsoleApplication1
            {
                public class MyClass
                {
                    private MyStruct myStruct = null;
                    private struct MyStruct
                    {
                        public int Value;
                    }
                }        
            }";
            const string source2 = @"
            namespace ConsoleApplication1
            {
                public class MyClass
                {
                    private readonly MyStruct myStruct = null;
                    private struct MyStruct
                    {
                        public int Value;
                    }
                }        
            }";
            await VerifyCSharpFixAsync(source1, source2, 0);
        }
        [Fact]
        public async Task structDefaultCreateWithoutReadOnlyDeclarationSameClass()
        {
            const string source1 = @"
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
    }";
            const string source2 = @"
    namespace ConsoleApplication1
    {
        public class MyClass
        {
            private readonly MyStruct myStruct = default(MyStruct);
            private struct MyStruct
            {
                public int Value;
            }
        }        
    }";
            await VerifyCSharpFixAsync(source1, source2, 0);
        }

        [Fact]
        public async Task structCreateWithoutReadonlyDeclaration()
        {
            const string source1 = @"
    namespace ConsoleApplication1
    {
        public class MyClass
        {
            private MyStruct myStruct = new MyStruct();
        }
        private struct MyStruct
        {
            public int Value;
        }
    }";
            const string source2 = @"
    namespace ConsoleApplication1
    {
        public class MyClass
        {
            private readonly MyStruct myStruct = new MyStruct();
        }
        private struct MyStruct
        {
            public int Value;
        }
    }";
            await VerifyCSharpFixAsync(source1, source2, 0);
        }

        [Fact]
        public async Task structDefaultCreateWithoutReadonlyDeclaration()
        {
            const string source1 = @"
    namespace ConsoleApplication1
    {
        public class MyClass
        {
            private MyStruct myStruct = default(MyStruct);
        }
        private struct MyStruct
        {
            public int Value;
        }
    }";
            const string source2 = @"
    namespace ConsoleApplication1
    {
        public class MyClass
        {
            private readonly MyStruct myStruct = default(MyStruct);
        }
        private struct MyStruct
        {
            public int Value;
        }
    }";
            await VerifyCSharpFixAsync(source1, source2, 0);
        }
    }
}
