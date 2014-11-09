namespace TestHelper
{
    public abstract partial class DiagnosticVerifier
    {
        protected void VerifyCSharpHasNoDiagnostics(string source)
        {
            VerifyCSharpDiagnostic(source);
        }
    }
}