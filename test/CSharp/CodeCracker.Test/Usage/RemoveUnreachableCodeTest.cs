using CodeCracker.CSharp.Usage;
using Microsoft.CodeAnalysis.CodeFixes;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class RemoveUnreachableCodeTest : CodeFixVerifier
    {
        protected override CodeFixProvider GetCodeFixProvider() => new RemoveUnreachableCodeCodeFixProvider();

        [Fact]
        public async void FixUnreacheableVariableDeclaration()
        {
            const string source = @"
class Foo
{
    void Method()
    {
        return;
        var a = 1;
    }
}";
            const string fixtest = @"
class Foo
{
    void Method()
    {
        return;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void FixUnreacheableInvocation()
        {
            const string source = @"
class Foo
{
    void F() { }
    void Method()
    {
        return;
        F();
    }
}";
            const string fixtest = @"
class Foo
{
    void F() { }
    void Method()
    {
        return;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void FixUnreacheableInvocationWithMemberAccess()
        {
            const string source = @"
class Foo
{
    void Method()
    {
        return;
        System.Diagnostics.Debug.WriteLine("""");
    }
}";
            const string fixtest = @"
class Foo
{
    void Method()
    {
        return;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void FixUnreacheableFor()
        {
            const string source = @"
class Foo
{
    void Method()
    {
        return;
        for(;;) { }
    }
}";
            const string fixtest = @"
class Foo
{
    void Method()
    {
        return;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void FixUnreacheableInvocationInsideFor()
        {
            const string source = @"
class Foo
{
    void F() { }
    void T()
    {
        for (int i = 0; i < 0; F())
        {
            return;
        }
    }
}";
            const string fixtest = @"
class Foo
{
    void F() { }
    void T()
    {
        for (int i = 0; i < 0; )
        {
            return;
        }
    }
}";
            await VerifyCSharpFixAsync(source, fixtest, formatBeforeCompare: false);
        }

        [Fact]
        public async void FixUnreacheableIncrement()
        {
            const string source = @"
class Foo
{
    void T()
    {
        for (int i = 0; i < 0; i++)
        {
            return;
        }
    }
}";
            const string fixtest = @"
class Foo
{
    void T()
    {
        for (int i = 0; i < 0; )
        {
            return;
        }
    }
}";
            await VerifyCSharpFixAsync(source, fixtest, formatBeforeCompare: false);
        }

        [Fact]
        public async void FixUnreacheableInIf()
        {
            const string source = @"
class Foo
{
    void T()
    {
        if (false) return 1;
        return 0;
    }
}";
            const string fixtest = @"
class Foo
{
    void T()
    {
        if (false)
        {
        }

        return 0;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void FixUnreacheableInElse()
        {
            const string source = @"
class Foo
{
    void T()
    {
        if (true)
            return 1;
        else
            return 0;
    }
}";
            const string fixtest = @"
class Foo
{
    void T()
    {
        if (true)
            return 1;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void FixUnreacheableInWhile()
        {
            const string source = @"
class Foo
{
    void T()
    {
        while (false) return 1;
        return 0;
    }
}";
            const string fixtest = @"
class Foo
{
    void T()
    {
        while (false)
        {
        }

        return 0;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void FixUnreacheableInLambda()
        {
            const string source = @"
class Foo
{
    void T()
    {
        System.Func<int> q13 = ()=>{ if (false) return 1; return 0; };
    }
}";
            const string fixtest = @"
class Foo
{
    void T()
    {
        System.Func<int> q13 = ()=>{ if (false) { } return 0; };
    }
}";
            await VerifyCSharpFixAsync(source, fixtest, formatBeforeCompare: false);
        }

        [Fact]
        public async void FixUnreacheableLambda()
        {
            const string source = @"
class Foo
{
    void T()
    {
        return;
        Action f = () => { };
    }
}";
            const string fixtest = @"
class Foo
{
    void T()
    {
        return;
    }
}";
            await VerifyCSharpFixAsync(source, fixtest, formatBeforeCompare: false);
        }

        [Fact]
        public async void FixAllUnreacheableCode()
        {
            const string source = @"
class Foo
{
    void Method()
    {
        return;
        var a = 1;
        var b = 1;
    }
}";
            const string fixtest = @"
class Foo
{
    void Method()
    {
        return;
    }
}";
            await VerifyFixAllAsync(source, fixtest);
        }
    }
}