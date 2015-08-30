using CodeCracker.CSharp.Usage;
using Xunit;

namespace CodeCracker.Test.CSharp.Usage
{
    public class RemovePrivateMethodNeverUsedAnalyzerTest : CodeFixVerifier<RemovePrivateMethodNeverUsedAnalyzer, RemovePrivateMethodNeverUsedCodeFixProvider>
    {
        [Fact]
        public async void DoesNotGenerateDiagnostics()
        {
            const string test = @"
  public class Foo
{
    public void PublicFoo()
    {
        PrivateFoo();
    }

    private void PrivateFoo()
    {
       PrivateFoo2();
    }

    private void PrivateFoo2() { }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void DoesNotGenerateDiagnosticsWhenPrivateMethodIsInvokedInPartialClasses()
        {
            const string test = @"
public partial class Foo
{
    public void PublicFoo()
    {
        PrivateFoo();
    }
}

public partial class Foo
{
    private void PrivateFoo()
    {
    }
}
";

            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async void DoesNotGenerateDiagnosticsWhenPrivateMethodIsInvokedInPartialClasses2()
        {
            const string test = @"
public partial class foo
{
    public foo()
    {

    }

    private void test()
    {
    }
}

public partial class foo
{
    public void test2()
    {
        test();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }


        [Fact]
        public async void FixRemovesPrivateMethodWhenItIsNotInvokedInPartialClasses()
        {
            const string test = @"
public partial class Foo
{
    public void PublicFoo()
    {
    }
}

public partial class Foo
{
    private void PrivateFoo()
    {
    }
}
";

            const string expected = @"
public partial class Foo
{
    public void PublicFoo()
    {
    }
}

public partial class Foo
{
}
";

            await VerifyCSharpFixAsync(test, expected);
        }

        [Fact]
        public async void WhenPrivateMethodUsedDoesNotGenerateDiagnostics()
        {
            const string test = @"
  public class Foo
{
    public void PublicFoo()
    {
        PrivateFoo();
    }

    private void PrivateFoo() { }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void WhenPrivateMethodUsedInAttributionDoesNotGenerateDiagnostics()
        {
            const string test = @"
using System;

public class Foo
{
    public void PublicFoo()
    {
        Action method = PrivateFoo;
    }

    private void PrivateFoo() { }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(test);
        }

        [Fact]
        public async void WhenPrivateMethodDoesNotUsedShouldCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    private void PrivateFoo() { }
}";
            const string fixtest = @"
class Foo
{
}";
            await VerifyCSharpFixAsync(source, fixtest);

        }

        [Fact]
        public async void GenericMethodDoesNotCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    void PrivateFoo<T>() { }
    public void Go()
    {
        PrivateFoo<int>();
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void GenericMethodWithConstraintDoesNotCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    void PrivateFoo<T>() where T : Foo { }
    public void Go()
    {
        PrivateFoo<Foo>();
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void StaticMethodDoesNotCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    static void PrivateFoo() { }
    public void Go()
    {
        PrivateFoo();
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void PrivateGenericStaticWithConstraintDoesNotCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    private static SymbolAnalysisContext GetSymbolAnalysisContext<T>(string code, string fileName = ""a.cs"") where T : SyntaxNode
    {
    }
    public void Go()
    {
        GetSymbolAnalysisContext<ClassDeclarationSyntax>(""class TypeName { }"", ""TemporaryGeneratedFile_.cs"");
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void MainMethodEntryPointReturningVoidDoesNotCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    static void Main(String[] args)
    {
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void MainMethodEntryPointReturningIntegerDoesNotCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    static int Main(string[] args)
    {
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void MainMethodEntryPointWithoutParameterDoesNotCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    static int Main()
    {
    }
}
";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void MainMethodEntryPointWithoutStaticModifierShouldCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    int Main(string[] args)
    {
    }
}
";
            const string fixtest = @"
class Foo
{
}
";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void MainMethodEntryPointWithMoreThanOneParameterShouldCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    static int Main(string[] args, string[] args2)
    {
    }
}
";
            const string fixtest = @"
class Foo
{
}
";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void MainMethodEntryPointWithDifferentParameterShouldCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    static int Main(string args)
    {
    }
}
";
            const string fixtest = @"
class Foo
{
}
";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void MainMethodEntryPointWithDifferentReturnTypeShouldCreateDiagnostic()
        {
            const string source = @"
class Foo
{
    static string Main(string[] args)
    {
    }
}
";
            const string fixtest = @"
class Foo
{
}
";
            await VerifyCSharpFixAsync(source, fixtest);
        }

        [Fact]
        public async void ExplicitlyImplementedInterfaceMethodDoesNotCreateDiagnostic()
        {
            const string source = @"
public class Foo : System.IEquatable<Foo>
{
    bool System.IEquatable<Foo>.Equals(Foo other)
    {
        throw new System.NotImplementedException();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }

        [Fact]
        public async void ImplicitlyImplementedInterfaceMethodDoesNotCreateDiagnostic()
        {
            const string source = @"
public interface IDoIt
{
    void DoItNow();
}

public class Foo : IDoIt
{
    public void DoItNow()
    {
        throw new System.NotImplementedException();
    }
}";
            await VerifyCSharpHasNoDiagnosticsAsync(source);
        }
    }
}