using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Style
{
    public class SwitchToAutoPropTests : CodeFixVerifier<SwitchToAutoPropAnalyzer, SwitchToAutoPropCodeFixProvider>
    {
        [Fact]
        public async Task IgnoresExistingAutoProp()
        {
            var source = @"public int Id { get; set; }".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresPropertyThatDoesMoreInTheGetter()
        {
            var source = @"
        private int id;
        public int Id
        {
            get
            {
                Console.WriteLine(1);
                return id;
            }
            set { id = value; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresPropertyThatDoesMoreInTheSetter()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { return id; }
            set
            {
                Console.WriteLine(1);
                id = value;
            }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresEmptyBodyInTheGetter()
        {
            var source = @"
        private int id;
        public int Id
        {
            get {  }
            set { id = value; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresEmptyBodyInTheSetter()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { return id; }
            set { }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresReadonlyProp()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { return id; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresWriteonlyProp()
        {
            var source = @"
        private int id;
        public int Id
        {
            set { id = value; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresSetterThatDoesNotSet()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { return id; }
            set { Console.WriteLine(1); }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresGetterThatDoesNotGet()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { throw new Exception(); }
            set { id = value; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task DoubleGetterDoesNotThrow()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { throw new Exception(); }
            get { throw new Exception(); }
            set { id = value; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresGetterThatDoesNotReturnTheSameFieldThatSetterSets()
        {
            var source = @"
        private int id;
        private int otherId;
        public int Id
        {
            get { return otherId; }
            set { id = value; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresSetterThatAssignsAnotherIdentifier()
        {
            var source = @"
        private int id;
        private int otherId;
        public int Id
        {
            get { return id; }
            set { id = otherId; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresSetterThatAssignsSomethingThatIsNotAnIdentifier()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { return id; }
            set { id = 1; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresWhenAssigningToSomethingThatIsNotAField()
        {
            var source = @"
        public int OtherId { get; set; }
        public int Id
        {
            get { return OtherId; }
            set { OtherId = value; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresWhenAssigningToAFieldFromAnotherClass()
        {
            const string source = @"
using System;

namespace ConsoleApplication1
{
    class AnotherType
    {
        public static int otherId = 1;
    }
    class TypeName
    {
        public int Id
        {
            get { return AnotherType.otherId; }
            set { AnotherType.otherId = value; }
        }
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresWhenAssigningToAFieldFromAnotherClassWithUsingStatic()
        {
            const string source = @"
namespace ConsoleApplication1
{
    using static AnotherType;
    class AnotherType
    {
        public static int otherId = 1;
    }
    class TypeName
    {
        public int Id
        {
            get { return otherId; }
            set { otherId = value; }
        }
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task IgnoresWhenNotCSharp6OrGreaterAndFieldHasAssignment()
        {
            var source = @"
        private int id = 1;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }
".WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source, LanguageVersion.CSharp5);
        }

        [Fact]
        public async Task SimplePropertyCreatesDiagnostic()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }
".WrapInCSharpClass();
            var expected = new DiagnosticResult(DiagnosticId.SwitchToAutoProp.ToDiagnosticId(), DiagnosticSeverity.Info)
                .WithLocation(10, 9)
                .WithMessage(string.Format(SwitchToAutoPropAnalyzer.MessageFormat.ToString(), "Id"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task FixSimplePropIntoAutoProp()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }//comment 1
".WrapInCSharpClass();
            var expected = @"public int Id { get; set; }//comment 1
".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FixSimplePropWithFieldAssigmentIntoAutoProp()
        {
            var source = @"
        private int id = 42;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }//comment 1
".WrapInCSharpClass();
            var expected = @"public int Id { get; set; } = 42;//comment 1
".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FixSimplePropWithMultipleFieldsIntoAutoProp()
        {
            const string source = @"
                private int x = 42, y; //comment 1
                public int X
                {
                    get { return x; }
                    set { x = value; }
                }";

            const string expected = @"
                private int y; //comment 1
                public int X { get; set; } = 42;";

            await VerifyCSharpFixAsync(source.WrapInCSharpClass(), expected.WrapInCSharpClass());
        }

        [Fact]
        public async Task FixSimplePropWithVaribleDeclarationNotFirstIntoAutoProp()
        {
            const string source = @"
                private int x, y = 42; //comment 1
                public int Y
                {
                    get { return y; }
                    set { y = value; }
                }";

            const string expected = @"
                private int x; //comment 1
                public int Y { get; set; } = 42;";

            await VerifyCSharpFixAsync(source.WrapInCSharpClass(), expected.WrapInCSharpClass());
        }

        [Fact]
        public async Task FixSimplePropWithMultipleFieldsIntoAutoPropAllInDoc()
        {
            var source = @"
                private int x = 42, y;
                private int z = int.MaxValue;
                public int X
                {
                    get { return x; }
                    set { x = value; }
                }
                public int Y
                {
                    get { return y; }
                    set { y = value; }
                }
                public int Z
                {
                    get { return z; }
                    set { z = value; }
                }".WrapInCSharpClass();
            var expected = @"public int X { get; set; } = 42;
                public int Y { get; set; }
                public int Z { get; set; } = int.MaxValue;".WrapInCSharpClass();
            await VerifyCSharpFixAllAsync(source, expected);
        }

        [Fact]
        public async Task FixSimplePropWithMultipleFieldsIntoAutoPropAllInSolution()
        {
            var source1 = @"
                void Foo()
                {
                    var other = new OtherType();
                    other.z = 1;
                }
                public int x = 42, y;
                public int X
                {
                    get { return x; }
                    set { x = value; }
                }
                public int Y
                {
                    get { return y; }
                    set { y = value; }
                }".WrapInCSharpClass();
            var source2 = @"
                void Foo()
                {
                    var type = new TypeName();
                    type.x = 1;
                    type.y = 2;
                }
                public int z = int.MaxValue;
                public int Z
                {
                    get { return z; }
                    set { z = value; }
                }".WrapInCSharpClass("OtherType");
            var expected1 = @"
                void Foo()
                {
                    var other = new OtherType();
                    other.Z = 1;
                }
                public int X { get; set; } = 42;
                public int Y { get; set; }".WrapInCSharpClass();
            var expected2 = @"
                void Foo()
                {
                    var type = new TypeName();
                    type.X = 1;
                    type.Y = 2;
                }
                public int Z { get; set; } = int.MaxValue;".WrapInCSharpClass("OtherType");
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { expected1, expected2 });
        }

        [Fact]
        public async Task FixPropIntoAutoPropAndFixFieldReferencesInSameClass()
        {
            var source = @"
        private int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }//comment 1
        public void Foo()
        {
            var someId = id;
            id = someId + 1;
        }
".WrapInCSharpClass();
            var expected = @"public int Id { get; set; }//comment 1
        public void Foo()
        {
            var someId = Id;
            Id = someId + 1;
        }
".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, expected);
        }

        [Fact]
        public async Task FixPropIntoAutoPropAndFixFieldReferencesInDifferentDocs()
        {
            var source1 = @"
        public int id;
        public int Id
        {
            get { return id; }
            set { id = value; }
        }
".WrapInCSharpClass();
            var source2 = @"
        public void Foo()
        {
            var c = new TypeName();
            var someId = c.id;
            c.id = someId + 1;
        }
".WrapInCSharpClass("OtherType");
            var expected1 = @"public int Id { get; set; }
".WrapInCSharpClass();
            var expected2 = @"
        public void Foo()
        {
            var c = new TypeName();
            var someId = c.Id;
            c.Id = someId + 1;
        }
".WrapInCSharpClass("OtherType");
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { expected1, expected2 });
        }

        [Fact]
        public async Task FixPropIntoAutoPropAndFixKeepingXMLComments()
        {
            const string source = @"
        public class Foo
        {
            private int x = 10;
            /// <summary>
            /// comment 1
            /// </summary>
            public int X
            {
                get { return x; }
                set { x = value; }
            }
        }";

            const string expected = @"
        public class Foo
        {
            /// <summary>
            /// comment 1
            /// </summary>
            public int X { get; set; } = 10;
        }";
            await VerifyCSharpFixAllAsync(source, expected);
        }

        [Fact]
        public async Task FixPropIntoAutoPropWithThisInSet()
        {
            var source = @"
        private string text;
        public string Text
        {
            get
            {
                return text;
            }

            set
            {
                this.text = value;
            }
        }"
.WrapInCSharpClass();

            var expected = @"public string Text{ get; set; }"
.WrapInCSharpClass();
            await VerifyCSharpFixAllAsync(source, expected);
        }

        [Fact]
        public async Task FixPropIntoAutoPropWithThisInGet()
        {
            var source = @"
        private string text;
        public string Text
        {
            get
            {
                return this.text;
            }

            set
            {
                text = value;
            }
        }
".WrapInCSharpClass();

            var expected = @"public string Text{ get; set; }
".WrapInCSharpClass();
            await VerifyCSharpFixAllAsync(source, expected);
        }

        [Fact]
        public async Task FixExplicitPropertyWithReferenceOnSameTime()
        {
            const string source = @"
interface IFoo
{
    int P { get; set; }
}
class Foo : IFoo
{
    public Foo()
    {
        p = 1;
    }
    private int p;
    int IFoo.P
    {
        get
        {
            return p;
        }
        set
        {
            p = value;
        }
    }
}";
            const string expected = @"
interface IFoo
{
    int P { get; set; }
}
class Foo : IFoo
{
    public Foo()
    {
        ((IFoo)this).P = 1;
    }
    int IFoo.P { get; set; }
}";
            await VerifyCSharpFixAllAsync(source, expected);
        }

        [Fact]
        public async Task FixExplicitPropertyWithReferenceOnDifferentType()
        {
            const string source = @"
interface IFoo
{
    int P { get; set; }
}
class Foo : IFoo
{
    public int p;
    int IFoo.P
    {
        get
        {
            return p;
        }
        set
        {
            p = value;
        }
    }
}
class Bar
{
    static void Baz()
    {
        var foo = new Foo();
        foo.p = 1;
    }
}";
            const string expected = @"
interface IFoo
{
    int P { get; set; }
}
class Foo : IFoo
{
    int IFoo.P { get; set; }
}
class Bar
{
    static void Baz()
    {
        var foo = new Foo();
        ((IFoo)foo).P = 1;
    }
}";
            await VerifyCSharpFixAllAsync(source, expected);
        }

        [Fact]
        public async Task FixAllExplicitPropertyWithReferenceOnDifferentNamespace()
        {
            const string source1 = @"
using Ns1;
namespace Ns1
{
    interface IFoo
    {
        int P { get; set; }
    }
}
namespace Ns2
{
    class Foo : IFoo
    {
        public int p;
        int IFoo.P
        {
            get
            {
                return p;
            }
            set
            {
                p = value;
            }
        }
    }
}";
            const string source2 = @"
using Ns2;
namespace Ns3
{
    class Bar
    {
        static void Baz()
        {
            var foo = new Foo();
            foo.p = 1;
        }
    }
}";
            const string expected1 = @"
using Ns1;
namespace Ns1
{
    interface IFoo
    {
        int P { get; set; }
    }
}
namespace Ns2
{
    class Foo : IFoo
    {
        int IFoo.P { get; set; }
    }
}";
            const string expected2 = @"
using Ns2;
namespace Ns3
{
    class Bar
    {
        static void Baz()
        {
            var foo = new Foo();
            ((Ns1.IFoo)foo).P = 1;
        }
    }
}";
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { expected1, expected2 });
        }

        [Fact]
        public async Task FixAllExplicitPropertyWithReferenceOnDifferentNamespaceWithImportedNs()
        {
            const string source1 = @"
using Ns1;
namespace Ns1
{
    interface IFoo
    {
        int P { get; set; }
    }
}
namespace Ns2
{
    class Foo : IFoo
    {
        public int p;
        int IFoo.P
        {
            get
            {
                return p;
            }
            set
            {
                p = value;
            }
        }
    }
}";
            const string source2 = @"
using Ns1;
using Ns2;
namespace Ns3
{
    class Bar
    {
        static void Baz()
        {
            var foo = new Foo();
            foo.p = 1;
        }
    }
}";
            const string expected1 = @"
using Ns1;
namespace Ns1
{
    interface IFoo
    {
        int P { get; set; }
    }
}
namespace Ns2
{
    class Foo : IFoo
    {
        int IFoo.P { get; set; }
    }
}";
            const string expected2 = @"
using Ns1;
using Ns2;
namespace Ns3
{
    class Bar
    {
        static void Baz()
        {
            var foo = new Foo();
            ((IFoo)foo).P = 1;
        }
    }
}";
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { expected1, expected2 });
        }

        [Fact]
        public async Task FixAllWithNonPublicProperty()
        {
            const string source1 = @"
namespace Ns1
{
    class Foo
    {
        public int p;
        private int P
        {
            get
            {
                return p;
            }
            set
            {
                p = value;
            }
        }
    }
}";
            const string source2 = @"
using Ns1;
namespace Ns2
{
    class Bar
    {
        static void Baz()
        {
            var foo = new Foo();
            foo.p = 1;
        }
    }
}";
            const string expected1 = @"
namespace Ns1
{
    class Foo
    {
        public int P { get; set; }
    }
}";
            const string expected2 = @"
using Ns1;
namespace Ns2
{
    class Bar
    {
        static void Baz()
        {
            var foo = new Foo();
            foo.P = 1;
        }
    }
}";
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { expected1, expected2 });
        }

        [Fact]
        public async Task FixKeepsStaticModifier()
        {
            const string source = @"
                private int y;
                public virtual int Y
                {
                    get { return y; }
                    set { y = value; }
                }";
            const string expected = @"public virtual int Y { get; set; }";
            await VerifyCSharpFixAsync(source.WrapInCSharpClass(), expected.WrapInCSharpClass());
        }

        [Fact]
        public async Task FixKeepsVirtualModifier()
        {
            const string source = @"
                private static int y;
                public static int Y
                {
                    get { return y; }
                    set { y = value; }
                }";
            const string expected = @"public static int Y { get; set; }";
            await VerifyCSharpFixAsync(source.WrapInCSharpClass(), expected.WrapInCSharpClass());
        }

        [Fact]
        public async Task FixUpdatesDerivedClassesCorrectly()
        {
            const string source = @"
class Point
{
    protected int x;
    public virtual int X
    {
        get
        {
            return x;
        }
        set
        {
            x = value;
        }
    }
}
class NewPoint : Point
{
    public override int X => (x - 15);
}";
            const string expected = @"
class Point
{
    public virtual int X { get; set; }
}
class NewPoint : Point
{
    public override int X => (base.X - 15);
}";
            await VerifyCSharpFixAsync(source, expected);
        }
    }
}