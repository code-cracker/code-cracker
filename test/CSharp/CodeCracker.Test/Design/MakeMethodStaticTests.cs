using CodeCracker.CSharp.Design;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Testing;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Design
{
    public class MakeMethodStaticTests : CodeFixVerifier<MakeMethodStaticAnalyzer, MakeMethodStaticCodeFixProvider>
    {
        [Theory]
        [InlineData(@"static void Foo() { }")]
        [InlineData(@"public virtual void Foo() { }")]
        [InlineData(@"string i; void Foo() { Console.WriteLine(i); }")]
        [InlineData(@"string i; void Foo() { i = """"; }")]
        [InlineData(@"string i; string Foo() { return i; }")]
        [InlineData(@"string i;
void Foo()
{
    if (System.DateTime.Now.Seconds > 5)
    {
        Console.WriteLine(i);
    }
}")]
        public async Task NoDiagnostic(string code)
        {
            var source = code.WrapInCSharpClass();
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoDiagnosticOnNew()
        {
            const string source = @"
        class B
        {
            private int i = 1;
            public int Foo() => i;
        }
        class C : B
        {
            public new int Foo() => 1;
        }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoDiagnosticOnOverrideAndAbstract()
        {
            const string source = @"
        abstract class B
        {
            public abstract void Foo();
        }
        class C : B
        {
            public override void Foo() { }
        }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoDiagnosticOnPartial()
        {
            const string source = @"
        partial class C
        {
            partial void Foo() { }
            partial void Foo();
        }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoDiagnosticOnExplicitInterfaceImplementation()
        {
            const string source = @"
        interface I
        {
            int Foo();
        }
        class C : I
        {
            int I.Foo() => 42;
        }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Theory]
        [InlineData("void Foo() { }")]
        [InlineData(@"void Foo()
{
    Console.WriteLine(1);
}")]
        [InlineData(@"void Foo()
{
    Console.WriteLine(i);
}
static string i;")]
        [InlineData("void Foo() => Console.WriteLine(1);")]
        public async Task WithDiagnostic(string code)
        {
            var source = code.WrapInCSharpClass();
            var expected = new DiagnosticResult(DiagnosticId.MakeMethodStatic.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(8, 14)
                .WithMessage(string.Format(MakeMethodStaticAnalyzer.MessageFormat, "Foo"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Theory]
        [InlineData("void Foo() { }", "static void Foo() { }")]
        [InlineData("int Foo() => 1;", "static int Foo() => 1;")]
        [InlineData(@"
#region
    void Foo() { }
#endregion", @"
#region
    static void Foo() { }
#endregion")]
        [InlineData(@"
///<summary>Method summary</summary>
void Foo() { }",
            @"
///<summary>Method summary</summary>
static void Foo() { }")]
        [InlineData(@"
///<summary>Method summary</summary>
public void Foo() { }",
            @"
///<summary>Method summary</summary>
public static void Foo() { }")]
        public async Task FixMakeMethodStaticWithoutReference(string code, string fix)
        {
            var source = code.WrapInCSharpClass();
            var fixtest = fix.WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task MakeMethodStaticWithReference()
        {
            var source = @"void Foo() { }
void Bar()
{
    Foo();
}".WrapInCSharpClass();
            var fixtest = @"static void Foo() { }
void Bar()
{
    Foo();
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task MakeMethodStaticWithReferenceWithThis()
        {
            var source = @"void Foo() { }
void Bar()
{
    this.Foo();
}".WrapInCSharpClass();
            var fixtest = @"static void Foo() { }
void Bar()
{
    Foo();
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task MakeMethodStaticWithReferenceAndLongNames()
        {
            var source = @"int Foo() => 1;
void Bar()
{
    var result = this.Foo() + this.Foo();
}".WrapInCSharpClass();
            var fixtest = @"static int Foo() => 1;
void Bar()
{
    var result = Foo() + Foo();
}".WrapInCSharpClass();
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task MakeMethodStaticWithReferenceInDifferentDocs()
        {
            var source1 = @"private int i;
void Bar()
{
    i = 1;
    var t = new Type2();
    t.Foo();
}".WrapInCSharpClass("Type1");
            var source2 = @"public void Foo() { }".WrapInCSharpClass("Type2");
            var fixtest1 = @"private int i;
void Bar()
{
    i = 1;
    var t = new Type2();
    Type2.Foo();
}".WrapInCSharpClass("Type1");
            var fixtest2 = @"public static void Foo() { }".WrapInCSharpClass("Type2");
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { fixtest1, fixtest2 });
        }

        [Fact]
        public async Task MakeMethodStaticWithReferenceInDifferentDocsWithCallsOnTheSameLine()
        {
            var source1 = @"private int i;
void Bar()
{
    i = 1;
    var t = new LargeTypeName();
    var result = t.Foo() + t.Foo();
}".WrapInCSharpClass("Type1");
            var source2 = @"public int Foo() { return 1; }".WrapInCSharpClass("LargeTypeName");
            var fixtest1 = @"private int i;
void Bar()
{
    i = 1;
    var t = new LargeTypeName();
    var result = LargeTypeName.Foo() + LargeTypeName.Foo();
}".WrapInCSharpClass("Type1");
            var fixtest2 = @"public static int Foo() { return 1; }".WrapInCSharpClass("LargeTypeName");
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { fixtest1, fixtest2 });
        }

        [Fact]
        public async Task MakeMethodStaticWhenReferencingAsAMethodGroup()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public virtual void Foo()
            {
                Func<int> i = Bar;
            }
            public int Bar() => 1;
        }
        class Context
        {
            private int i;
            public void Register(Func<int> f) { i++; }
        }
    }";
            const string fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public virtual void Foo()
            {
                Func<int> i = Bar;
            }
            public static int Bar() => 1;
        }
        class Context
        {
            private int i;
            public void Register(Func<int> f) { i++; }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task MakeMethodWithLeadingTriviaStaticWhenReferencingAsAMethodGroup()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public virtual void Foo()
            {
                Func<int> i = Bar;
            }

            /// <summary>Bar method</summary>
            int Bar() => 1;
        }
        class Context
        {
            private int i;
            public void Register(Func<int> f) { i++; }
        }
    }";
            const string fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public virtual void Foo()
            {
                Func<int> i = Bar;
            }

            /// <summary>Bar method</summary>
            static int Bar() => 1;
        }
        class Context
        {
            private int i;
            public void Register(Func<int> f) { i++; }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task MakeMethodStaticWhenReferencingAsAMethodGroupPassedToAFunction()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public virtual void Foo(Context c) => c.Register(Bar);
            public int Bar() => 1;
        }
        class Context
        {
            private int i;
            public void Register(Func<int> f) { i++; }
        }
    }";
            const string fixtest = @"
    using System;
    namespace ConsoleApplication1
    {
        class TypeName
        {
            public virtual void Foo(Context c) => c.Register(Bar);
            public static int Bar() => 1;
        }
        class Context
        {
            private int i;
            public void Register(Func<int> f) { i++; }
        }
    }";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task NoDiagnosticWhenImplementingInterface()
        {
            const string source = @"
    using System;
    namespace ConsoleApplication1
    {
        interface ITypeName
        {
            int Bar();
        }
        class TypeName : ITypeName
        {
            public int Bar() => 1;
        }
    }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoDiagnosticOnXUnitTestMethods()
        {
            const string source = @"
        using Xunit;
        namespace ConsoleApplication1
        {
            class XUnitTests
            {
                [Fact]
                void FactMethod() { }

                [Theory]
                void TheoryMethod() { }

                [TheoryAttribute]
                void TheoryMethod2() { }
            }
        }";
            var xunitReference = MetadataReference.CreateFromFile(typeof(FactAttribute).Assembly.Location);
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoDiagnosticOnMicrosoftTestMethods()
        {
            const string source = @"
        using Microsoft.VisualStudio.TestTools.UnitTesting;
        namespace ConsoleApplication1
        {
            class MsTestTests
            {
                [TestMethod]
                void TestMethod() { }

                [AssemblyInitialize]
                void AssemblyInitializeMethod() { }

                [AssemblyCleanup]
                AssemblyCleanup() { }

                [ClassInitialize]
                void ClassInitializeMethod() { }

                [ClassCleanup]
                void ClassCleanupMethod() { }

                [TestInitialize]
                void TestInitializeMethod() { }

                [TestCleanup]
                void TestCleanupMethod() { }
            }
        }";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task NoDiagnosticOnNUnitTestMethods()
        {
            const string nunitNonTestFixtureWithAttributesSource = @"
        using NUnit.Framework;
        namespace ConsoleApplication1
        {
            class nunitNonTestFixtureWithAttributesSource
            {
                [Test]
                void TestMethod() { }

                [TestCase]
                void TestCaseMethod() { }

                [TestCaseSource]
                void TestCaseSourceMethod() { }

                [TestFixtureSetup]
                void TestFixtureSetupMethod() { }

                [TestFixtureTeardown]
                void TestFixtureTeardownMethod() { }

                [SetUp]
                void SetUpMethod() { }

                [TearDown]
                void TearDownMethod() { }

                [OneTimeSetUp]
                void OneTimeSetUpMethod() { }

                [OneTimeTearDown]
                void OneTimeTearDownMethod() { }
            }
        }";

            const string nunitTestFixtureWithoutAttributesSource = @"
        using NUnit.Framework;

        namespace NUnit.Framework
        {
            public class TestFixtureAttribute : System.Attribute
            {
            }
        }

        namespace ConsoleApplication2
        {
            [TestFixture]
            class NUnitTestFixtureWithoutAttributes
            {
                void TestMethod() { }

                void MethodUnderTest() { }

                void MethodTestShouldPass() { }
            }
        }";

            const string nunitWithoutTestFixtureWithTestAttributeAndOtherNonAttributedMethodsSource = @"
        using NUnit.Framework;
        namespace ConsoleApplication3
        {
            class NUnitWithoutTestFixtureWithTestAttributeAndOtherNonAttributedMethods
            {
                [Test]
                void TestMethod() { }

                void MethodUnderTest() { }
            }
        }";

            const string nunitWithoutTestFixtureWithTestCaseAttributeAndOtherNonAttributedMethodsSource = @"
        using NUnit.Framework;
        namespace ConsoleApplication4
        {
            class NUnitWithoutTestFixtureWithTestCaseAttributeAndOtherNonAttributedMethods
            {
                [TestCase]
                void TestMethod() { }

                void MethodUnderTest() { }
            }
        }";

            const string nunitWithoutTestFixtureWithTestCaseSourceAttributeAndOtherNonAttributedMethodsSource = @"
        using NUnit.Framework;
        namespace ConsoleApplication5
        {
            class NUnitWithoutTestFixtureWithTestCaseSourceAttributeAndOtherNonAttributedMethods
            {
                [TestCaseSource]
                void TestMethod() { }

                void MethodUnderTest() { }
            }
        }";

            await VerifyCSharpHasNoDiagnosticsAsync(new string[] {
                nunitNonTestFixtureWithAttributesSource,
                nunitTestFixtureWithoutAttributesSource,
                nunitWithoutTestFixtureWithTestAttributeAndOtherNonAttributedMethodsSource,
                nunitWithoutTestFixtureWithTestCaseAttributeAndOtherNonAttributedMethodsSource,
                nunitWithoutTestFixtureWithTestCaseSourceAttributeAndOtherNonAttributedMethodsSource
            });
        }

        [Theory]
        [InlineData(@"void Application_AuthenticateRequest() { }")]
        [InlineData(@"void Application_BeginRequest() { }")]
        [InlineData(@"void Application_End() { }")]
        [InlineData(@"void Application_EndRequest() { }")]
        [InlineData(@"void Application_Error() { }")]
        [InlineData(@"void Application_Start(object sender, EventArgs e) { }")]
        [InlineData(@"void Session_End() { }")]
        [InlineData(@"void Session_Start() { }")]
        public async Task IgnoreKnownWebFormsMethods(string code)
        {
            var source =  $@"
    using System;
    namespace System.Web
    {{
        public class HttpApplication {{ }}
    }}
    namespace MyWebApp1
    {{
        public class Global : System.Web.HttpApplication
        {{
            {code}
        }}
    }}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task FixWhenUsedAsMethodGroup()
        {
            const string source = @"
class Bar
{
    void ShouldBeStatic()
    {
    }
    void Caller()
    {
        Foo.M(new Baz(ShouldBeStatic));
    }
}
class Foo
{
    public static void M(Baz b) { }
}
class Baz
{
    public Baz(Action a)
    {
    }
}";
            const string fixtest = @"
class Bar
{
    static void ShouldBeStatic()
    {
    }
    void Caller()
    {
        Foo.M(new Baz(ShouldBeStatic));
    }
}
class Foo
{
    public static void M(Baz b) { }
}
class Baz
{
    public Baz(Action a)
    {
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }


        [Fact]
        public async Task FixWhenUsedAsMethodGroupInMultipleDocs()
        {
            const string source1 = @"
namespace Ns
{
    class Bar
    {
        public void ShouldBeStatic()
        {
        }
    }
}";
            const string source2 = @"
namespace Ns
{
    class Foo
    {
        public static void M(Baz b) { }
    }
    class Baz
    {
        public Baz(Action a)
        {
        }
        static void Caller()
        {
            Foo.M(new Baz(new Bar().ShouldBeStatic));
        }
    }
}";
            const string fixtest1 = @"
namespace Ns
{
    class Bar
    {
        public static void ShouldBeStatic()
        {
        }
    }
}";
            const string fixtest2 = @"
namespace Ns
{
    class Foo
    {
        public static void M(Baz b) { }
    }
    class Baz
    {
        public Baz(Action a)
        {
        }
        static void Caller()
        {
            Foo.M(new Baz(Bar.ShouldBeStatic));
        }
    }
}";
            await VerifyCSharpFixAllAsync(new[] { source1, source2 }, new[] { fixtest1, fixtest2 });
        }

        [Fact]
        public async Task FixWhenUsedAsMethodGroupWithThis()
        {
            const string source = @"
class Bar
{
    void ShouldBeStatic()
    {
    }
    void Caller()
    {
        Foo.M(new Baz(this.ShouldBeStatic));
    }
}
class Foo
{
    public static void M(Baz b) { }
}
class Baz
{
    public Baz(Action a)
    {
    }
}";
            const string fixtest = @"
class Bar
{
    static void ShouldBeStatic()
    {
    }
    void Caller()
    {
        Foo.M(new Baz(ShouldBeStatic));
    }
}
class Foo
{
    public static void M(Baz b) { }
}
class Baz
{
    public Baz(Action a)
    {
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenUsedAsMethodGroupWithThisAndAnOverload()
        {
            const string source = @"
class Bar
{
    private int j;
    void ShouldBeStatic(int i)
    {
        j = i;
    }
    void ShouldBeStatic()
    {
    }
    void Caller()
    {
        Foo.M(new Baz(this.ShouldBeStatic));
    }
}
class Foo
{
    public static void M(Baz b) { }
}
class Baz
{
    public Baz(Action a)
    {
    }
}";
            const string fixtest = @"
class Bar
{
    private int j;
    void ShouldBeStatic(int i)
    {
        j = i;
    }
    static void ShouldBeStatic()
    {
    }
    void Caller()
    {
        Foo.M(new Baz(ShouldBeStatic));
    }
}
class Foo
{
    public static void M(Baz b) { }
}
class Baz
{
    public Baz(Action a)
    {
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWhenUsedWithParenthesisOnMethodGroup()
        {
            const string source = @"
class Bar
{
    void ShouldBeStatic()
    {
    }
    void Caller()
    {
        (this.ShouldBeStatic)();
    }
}";
            const string fixtest = @"
class Bar
{
    static void ShouldBeStatic()
    {
    }
    void Caller()
    {
        ShouldBeStatic();
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWithVariable()
        {
            const string source = @"
class Foo
{
    static void M()
    {
        var b = new Bar();
        b.M();
    }
}
class Bar
{
    public void M()
    {
    }
}";
            const string fixtest = @"
class Foo
{
    static void M()
    {
        var b = new Bar();
        Bar.M();
    }
}
class Bar
{
    public static void M()
    {
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWithNew()
        {
            const string source = @"
class Foo
{
    static void M()
    {
        new Bar().M();
    }
}
class Bar
{
    public void M()
    {
    }
}";
            const string fixtest = @"
class Foo
{
    static void M()
    {
        Bar.M();
    }
}
class Bar
{
    public static void M()
    {
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixInAHierarchy()
        {
            const string source = @"
class Foo
{
    public void M()
    {
    }
    class Bar
    {
        static void N()
        {
            new Foo().M();
        }
    }
}";
            const string fixtest = @"
class Foo
{
    public static void M()
    {
    }
    class Bar
    {
        static void N()
        {
            M();
        }
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixInAHierarchyWithNameClash()
        {
            const string source = @"
class Foo
{
    public void M()
    {
    }
    class Bar
    {
        static void M()
        {
            new Foo().M();
        }
    }
}";
            const string fixtest = @"
class Foo
{
    public static void M()
    {
    }
    class Bar
    {
        static void M()
        {
            Foo.M();
        }
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task FixWithAGetterMethod()
        {
            const string source = @"
class Foo
{
    public void M()
    {
    }
}
class Bar
{
    static void M()
    {
        GetFoo().M();
    }
    static Foo GetFoo() => new Foo();
}";
            const string fixtest = @"
class Foo
{
    public static void M()
    {
    }
}
class Bar
{
    static void M()
    {
        Foo.M();
    }
    static Foo GetFoo() => new Foo();
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async Task IgnoreInStructs()
        {
            const string source = @"
struct Foo
{
    private int x;

    public void M(int x)
    {
        this.x = x;
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ReportInStructs()
        {
            const string source = @"
struct Foo
{
    public void M()
    {
        N();
    }
    public static void N() { }
}";
            var expected = new DiagnosticResult(DiagnosticId.MakeMethodStatic.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(4, 17)
                .WithMessage(string.Format(MakeMethodStaticAnalyzer.MessageFormat, "M"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IgnoreForGetEnumerator()
        {
            const string source = @"
class Foo
{
    public System.Collections.IEnumerator GetEnumerator()
    {
        yield return 1;
        yield return 2;
    }
}";

            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async Task ReportForGetEnumeratorNotReturningIEnumerator()
        {
            const string source = @"
class Foo
{
    public void GetEnumerator()
    {
        N();
    }

    public static void N() { }
}";
            var expected = new DiagnosticResult(DiagnosticId.MakeMethodStatic.ToDiagnosticId(), DiagnosticSeverity.Warning)
                .WithLocation(4, 17)
                .WithMessage(string.Format(MakeMethodStaticAnalyzer.MessageFormat, "GetEnumerator"));
            await VerifyCSharpDiagnosticAsync(source, expected);
        }

        [Fact]
        public async Task IgnoreMethodsWithRoutedEventArgs()
        {
            const string source = @"
public class MainWindow
{
    void Window_Loaded(object sender, System.Windows.RoutedEventArgs e)
    {
    }
}
namespace System.Windows
{
    public class RoutedEventArgs : System.EventArgs { }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}