using System.Threading.Tasks;

namespace TestHelper
{
    public abstract partial class DiagnosticVerifier
    {
        protected async Task VerifyCSharpHasNoDiagnosticsAsync(string source) =>
            await VerifyCSharpDiagnosticAsync(source);
        protected async Task VerifyCSharpHasNoDiagnosticsAsync(params string[] sources) =>
            await VerifyCSharpDiagnosticAsync(sources);
    }
}