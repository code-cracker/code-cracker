using CodeCracker.CSharp.Refactoring;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TestHelper;
using Xunit;

namespace CodeCracker.CSharp.Test.Refactoring
{
    public class BaseAllowMembersOrderingCodeFixProviderTests :
        CodeFixTest<AllowMembersOrderingAnalyzer, BaseAllowMembersOrderingCodeFixProviderTests.MockCodeFixProvider>
    {
        [Theory]
        [InlineData("class", "void B() { };", "void A() { };")]
        [InlineData("struct", "int c = 3, d = 4;", "int a = 1, b = 2;")]
        public async Task BaseAllowMembersOrderingShouldCallIComparerToOrder(string typeDeclaration, string memberA, string memberB)
        {
            var codeFixProvider = base.GetCSharpCodeFixProvider() as MockCodeFixProvider;

            var source = @"
            public " + typeDeclaration + @" Foo
            {
                " + memberA + @"
                " + memberB + @"
            }";

            var expected = @"
            public " + typeDeclaration + @" Foo
            {
                " + memberB + @"
                " + memberA + @"
            }";

            await VerifyCSharpFixAsync(source, expected, codeFixProvider: codeFixProvider);
            Assert.True(codeFixProvider.HasIComparerBeenCalled, "The IComparer must be used to sort the members of that type");
        }

        [Fact]
        public async Task BaseAllowMembersOrderingShouldNotRegisterFixIfIsAlreadySorted()
        {
            const string source = @"
            public class Foo
            {
                int a;
                void B() { }
            }";
            await VerifyCSharpHasNoFixAsync(source);
        }

        [Theory]
        [InlineData("class")]
        [InlineData("struct")]
        public async Task BaseAllowMembersOrderingShouldSupportWriteMembers(string typeDeclaration)
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
                    void Method(string a) { }
                    public string this[int i] { set { } }
                    public string Property { get; set; }
                    private interface Interface { }
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
                    public string Field;
                }
            }";

            var expected = @"
            using System;

            namespace ConsoleApplication1
            {
                " + typeDeclaration + @" Foo
                {
                    private interface Interface { }
                    public class Foo2 { }
                    public delegate double Delegate(double num);
                    public enum Enum { Enum1, Enum2 = 1 }
                    public event Action Event
                    {
                        add { EventField += value; }
                        remove { EventField -= value; }
                    }
                    public event Action EventField1;
                    public Foo()
                    {
                        Property = Field1 = Field = "";
                        EventField = EventField1 = () => { };
                    }
                    public static Foo operator +(Foo f1, Foo f2) { return new Foo(); }
                    public string Field;
                    public string Property { get; set; }
                    public string this[int i] { set { } }
                    public struct Struct { }
                    void Method(string a) { }
                }
            }";

            await VerifyCSharpFixAsync(source, expected);
        }

        public class MockCodeFixProvider : BaseAllowMembersOrderingCodeFixProvider
        {
            public MockCodeFixProvider() : base("Fake codefix") { }

            public bool HasIComparerBeenCalled { get; private set; }

            protected override IComparer<MemberDeclarationSyntax> GetMemberDeclarationComparer(Document document, CancellationToken cancellationToken)
                => new AlphabeticalMemberOrderingComparer(this);

            internal class AlphabeticalMemberOrderingComparer : IComparer<MemberDeclarationSyntax>
            {
                readonly MockCodeFixProvider parent;

                public AlphabeticalMemberOrderingComparer(MockCodeFixProvider parent) { this.parent = parent; }

                public int Compare(MemberDeclarationSyntax x, MemberDeclarationSyntax y)
                {
                    parent.HasIComparerBeenCalled = true;
                    return string.Compare(x.ToFullString(), y.ToFullString(), System.StringComparison.InvariantCulture);
                }
            }
        }

    }
}