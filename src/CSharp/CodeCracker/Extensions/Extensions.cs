namespace CodeCracker
{
    public static class Extensions
    {
        public static string ToDiagnosticId(this DiagnosticId diagnosticId) => $"CC{(int)diagnosticId :D4}";
    }
}
