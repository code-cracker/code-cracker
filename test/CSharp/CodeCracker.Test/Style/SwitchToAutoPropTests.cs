using CodeCracker.CSharp.Style;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
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
            var expected = new DiagnosticResult
            {
                Id = DiagnosticId.SwitchToAutoProp.ToDiagnosticId(),
                Message = string.Format(SwitchToAutoPropAnalyzer.MessageFormat.ToString(), "Id"),
                Severity = DiagnosticSeverity.Info,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 9) }
            };
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


        [Fact(Skip = "Skip until the FixAll has been fixed.")]
        public async Task FixSimplePropWithMultipleFieldsIntoAutoPropAll()
        {
            const string source = @"
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
                }";

            const string expected = @"
                public int X { get; set; } = 42;
                public int Y { get; set; }
                public int Z { get; set; } = int.MaxValue;";

            await VerifyCSharpFixAllAsync(source.WrapInCSharpClass(), expected.WrapInCSharpClass());
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
            var source = @"
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

            var expected = @"
        public class Foo
        {
            /// <summary>
            /// comment 1
            /// </summary>
            public int X { get; set; } = 10;
        }";
            await VerifyCSharpFixAllAsync(source, expected);
        }
    }
}