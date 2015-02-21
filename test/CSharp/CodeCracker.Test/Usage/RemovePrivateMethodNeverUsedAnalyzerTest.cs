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
    }
}