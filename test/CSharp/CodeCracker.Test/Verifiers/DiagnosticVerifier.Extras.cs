using System.Threading.Tasks;

namespace CodeCracker.CSharp.Test
{
    public abstract partial class DiagnosticVerifier
    {
        protected async Task VerifyCSharpHasNoDiagnosticsAsync(string source) =>
            await VerifyCSharpDiagnosticAsync(source);
        protected async Task VerifyCSharpHasNoDiagnosticsAsync(params string[] sources) =>
            await VerifyCSharpDiagnosticAsync(sources);
    }
}