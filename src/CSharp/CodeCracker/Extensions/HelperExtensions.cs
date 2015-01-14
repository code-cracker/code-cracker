namespace CodeCracker
{
    public static class HelperExtensions
    {
        public static string ToDiagnosticId(this DiagnosticId diagnosticId) => $"CC{(int)diagnosticId :D4}";
    }
}
