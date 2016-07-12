using CodeCracker.CSharp.Performance;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using Xunit;

namespace CodeCracker.Test.CSharp.Performance
{
    public class SealMemberTests : CodeFixVerifier<SealMemberAnalyzer, SealMemberCodeFixProvider>
    {
        [Fact]
        public async Task IgnoreNonVirtualMethod()
        {
            const string test = @"
class AClass
{
    public virtual void Foo() { }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoreEventFieldWithMoreThanOneVariable()
        {
            const string test = @"
class Base
{
    public virtual event System.Action A, B;
}
class Derived : Base
{
    public override event System.Action A, B;
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoredDerivedClassWithNoOverrides()
        {
            const string test = @"
class Base { }
class Derived : Base
{
    public void Foo() { }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoredAlreadySealedMembers()
        {
            const string test = @"
class Base
{
    public virtual void Foo() { }
}
class Derived : Base
{
    public override sealed void Foo() { }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task IgnoredAlreadySealedClass()
        {
            const string test = @"
class Base
{
    public virtual void Foo() { }
}
sealed class Derived : Base
{
    public override void Foo() { }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async Task ShowsDiagnosticForDerivedMethod()
        {
            const string test = @"
class Base
{
    public virtual void Foo() { }
    public virtual void Bar() { }
    public virtual int Prop { get; set; }
    public virtual event System.Action A;
    public virtual event System.Action F { add { } remove { } }
}
class Derived : Base
{
    public override void Foo() { }
    public override sealed void Bar() { }
    public override int Prop { get; set; }
    public override event System.Action A;
    public override event System.Action F { add { } remove { } }
    public void Baz() { }
    public virtual void Ban() { }
}
";
            var classDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.SealMember.ToDiagnosticId(),
                Message = string.Format(SealMemberAnalyzer.ClassMessageFormat.ToString(), "Derived"),
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 10, 7) }
            };
            var methodDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.SealMember.ToDiagnosticId(),
                Message = string.Format(SealMemberAnalyzer.MemberMessageFormat.ToString(), "Foo"),
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 12, 5) }
            };
            var propertyDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.SealMember.ToDiagnosticId(),
                Message = string.Format(SealMemberAnalyzer.MemberMessageFormat.ToString(), "Prop"),
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 14, 5) }
            };
            var eventFieldDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.SealMember.ToDiagnosticId(),
                Message = string.Format(SealMemberAnalyzer.MemberMessageFormat.ToString(), "A"),
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 15, 5) }
            };
            var eventDeclarationDiagnostic = new DiagnosticResult
            {
                Id = DiagnosticId.SealMember.ToDiagnosticId(),
                Message = string.Format(SealMemberAnalyzer.MemberMessageFormat.ToString(), "F"),
                Severity = DiagnosticSeverity.Hidden,
                Locations = new[] { new DiagnosticResultLocation("Test0.cs", 16, 5) }
            };
            await VerifyCSharpDiagnosticAsync(test,
                expected: new[] { classDiagnostic, methodDiagnostic, propertyDiagnostic, eventFieldDiagnostic, eventDeclarationDiagnostic });
        }

        [Fact]
        public async Task SealMembers()
        {
            const string source = @"
class Base
{
    public virtual void Foo() { }
    public virtual void Bar() { }
}
class Derived : Base
{
    public override void Foo() { }
    //comment1
    public override /*comment3*/ sealed void Bar() { }//comment2
}
";
            const string fixtest = @"
class Base
{
    public virtual void Foo() { }
    public virtual void Bar() { }
}
class Derived : Base
{
    public override sealed void Foo() { }
    //comment1
    public override /*comment3*/ sealed void Bar() { }//comment2
}
";
            await VerifyCSharpFixAsync(source, fixtest, diagnosticIndex: 0);
        }

        [Fact]
        public async Task IgnoredOverridesOverridenMethods()
        {
            const string test = @"
class Grandparent
{
    public virtual void Foo() { }
}
class Parent : Grandparent
{
    public override void Foo() { }
}
sealed class Child : Parent
{
    public override sealed void Foo() { }
}
";
            await VerifyCSharpHasNoFixAsync(test);
        }

        [Fact]
        public async Task SealsOnlyAllowedMembers()
        {
            const string source = @"
class Grandparent
{
    public virtual void Foo() { }
    public virtual void Bar() { }
}
class Parent : Grandparent
{
    public override void Foo() { }
    public override void Bar() { }
}
class Child : Parent
{
}
sealed class ChildChild : Parent
{
    public override void Foo() { }
}
";
            const string fixtest = @"
class Grandparent
{
    public virtual void Foo() { }
    public virtual void Bar() { }
}
class Parent : Grandparent
{
    public override void Foo() { }
    public override sealed void Bar() { }
}
class Child : Parent
{
}
sealed class ChildChild : Parent
{
    public override void Foo() { }
}
";
            await VerifyCSharpFixAsync(source, fixtest, diagnosticIndex: 0);
        }

        [Fact]
        public async Task SealOnlyOneMember()
        {
            const string source = @"
class Base
{
    public virtual void Foo() { }
    public virtual void Bar() { }
}
class Derived : Base
{
    public override void Foo() { }
    public override void Bar() { }
}
";
            const string fixtest = @"
class Base
{
    public virtual void Foo() { }
    public virtual void Bar() { }
}
class Derived : Base
{
    public override sealed void Foo() { }
    public override void Bar() { }
}
";
            await VerifyCSharpFixAsync(source, fixtest, diagnosticIndex: 1);
        }

        [Fact]
        public async Task SealOnlyOneEvent()
        {
            const string source = @"
class Base
{
    public virtual event System.Action A;
    public virtual void Foo() { }
}
class Derived : Base
{
    public override event System.Action A;
    public override void Foo() { }
}
";
            const string fixtest = @"
class Base
{
    public virtual event System.Action A;
    public virtual void Foo() { }
}
class Derived : Base
{
    public override sealed event System.Action A;
    public override void Foo() { }
}
";
            await VerifyCSharpFixAsync(source, fixtest, diagnosticIndex: 1);
        }
    }
}