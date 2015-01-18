using CodeCracker.Refactoring;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.Test.Refactoring
{
    public class StyleCopAllowMembersOrderingCodeFixProviderTests : CodeFixTest<AllowMembersOrderingAnalyzer, StyleCopAllowMembersOrderingCodeFixProvider>
    {
        [Fact]
        public async Task StyleCopAllowMembersOrderingShouldAssureMembersOrderByType()
        {
            const string source = @"
            class Foo
            {
                public class Class { }
                public struct Sruct { }
                void Method() { }
                public int this[int a] { set { } }
                public int Property { set { } }
                public interface Interface { }
                enum Enum { Enum1 }
                public event System.Action Event { add { } remove { } }
                public delegate double Delegate();
                ~Foo() { }
                Foo() { }
                public event System.Action EventField;
                public int b = 0;
            }";

            const string expected = @"
            class Foo
            {
                public int b = 0;
                Foo() { }
                ~Foo() { }
                public delegate double Delegate();
                public event System.Action EventField;
                public event System.Action Event { add { } remove { } }
                enum Enum { Enum1 }
                public interface Interface { }
                public int Property { set { } }
                public int this[int a] { set { } }
                void Method() { }
                public struct Sruct { }
                public class Class { }
            }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task StyleCopAllowMembersOrderingShouldAssureMembersOrderByModifiers()
        {
            const string source = @"
            public class Foo
            {
                private int p = 0;
                protected int q = 0;
                protected internal int r = 0;
                internal int s = 0;
                public int t = 0;
                static int u = 0;
                public static int v = 0;
                const int x = 0;
                public const int z = 0;
            }";

            const string expected = @"
            public class Foo
            {
                public const int z = 0;
                public static int v = 0;
                public int t = 0;
                internal int s = 0;
                protected internal int r = 0;
                protected int q = 0;
                const int x = 0;
                static int u = 0;
                private int p = 0;
            }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task StyleCopAllowMembersOrderingShouldAssureMembersOrderByAlphabeticalOrder()
        {
            const string source = @"
            class Foo
            {
                int c = 2, d = 3;
                int a = 0, b = 1;
                event System.Action EventField1;
                event System.Action EventField;
                delegate double Delegate2();
                delegate double Delegate();
                event System.Action Event1 { add { } remove { } }
                event System.Action Event { add { } remove { } }
                enum Enum1 { Enum1 }
                enum Enum { Enum1 }
                interface Interface2 { }
                interface Interface { }
                int Property1 { set { } }
                int Property { set { } }
                public static int operator +(Foo a, Foo b) { return 0; }
                public static int operator -(Foo a, Foo b) { return 0; }
                void Method1() { }
                void Method() { }
                public struct Sruct1 { }
                public struct Sruct { }
                public class Class { }
                public class Class1 { }
            }";

            const string expected = @"
            class Foo
            {
                int a = 0, b = 1;
                int c = 2, d = 3;
                delegate double Delegate();
                delegate double Delegate2();
                event System.Action EventField;
                event System.Action EventField1;
                event System.Action Event { add { } remove { } }
                event System.Action Event1 { add { } remove { } }
                enum Enum { Enum1 }
                enum Enum1 { Enum1 }
                interface Interface { }
                interface Interface2 { }
                int Property { set { } }
                int Property1 { set { } }
                public static int operator -(Foo a, Foo b) { return 0; }
                public static int operator +(Foo a, Foo b) { return 0; }
                void Method() { }
                void Method1() { }
                public struct Sruct { }
                public struct Sruct1 { }
                public class Class { }
                public class Class1 { }
            }";

            await VerifyCSharpFixAsync(source, expected);
        }

        [Theory]
        [InlineData("struct")]
        [InlineData("class")]
        public async Task StyleCopAllowMembersOrderingShouldAssureMembersOrderByStyleCopPatterns(string typeDeclaration)
        {
            var source = @"
            using System;
            namespace ConsoleApplication1
            {
                " + typeDeclaration + @" Foo
                {
                    public class Foo2 { }
                    public struct Struct { }
                    public static Foo operator +(Foo f1, Foo f2) { return new Foo(); }
                    public static Foo operator -(Foo f1, Foo f2) { return new Foo(); }
                    void Method(string a) { }
                    internal void Method(int a) { }
                    public void Method1() { }
                    public void Method() { }
                    public string this[int i] { set { } }
                    public string Property { get; set; }
                    public interface Interface { }
                    public enum Enum { Enum1, Enum2 = 1 }
                    public event Action Event
                    {
                        add { EventField += value; }
                        remove { EventField -= value; }
                    }
                    public delegate double Delegate(double num);
                    public Foo()
                    {
                        Property = Field1 = Field = "";
                        EventField = EventField1 = () => { };
                    }
                    public event Action EventField1;
                    public event Action EventField;
                    public string Field;
                    public static string Field1;
                }
            }";

            var expected = @"
            using System;
            namespace ConsoleApplication1
            {
                " + typeDeclaration + @" Foo
                {
                    public static string Field1;
                    public string Field;
                    public Foo()
                    {
                        Property = Field1 = Field = "";
                        EventField = EventField1 = () => { };
                    }
                    public delegate double Delegate(double num);
                    public event Action EventField;
                    public event Action EventField1;
                    public event Action Event
                    {
                        add { EventField += value; }
                        remove { EventField -= value; }
                    }
                    public enum Enum { Enum1, Enum2 = 1 }
                    public interface Interface { }
                    public string Property { get; set; }
                    public string this[int i] { set { } }
                    public static Foo operator -(Foo f1, Foo f2) { return new Foo(); }
                    public static Foo operator +(Foo f1, Foo f2) { return new Foo(); }
                    public void Method() { }
                    public void Method1() { }
                    internal void Method(int a) { }
                    void Method(string a) { }
                    public struct Struct { }
                    public class Foo2 { }
                }
            }";

            await VerifyCSharpFixAsync(source, expected);
        }
    }
}