namespace CodeCracker
{
    static class HelpLink
    {
        public static string ForDiagnostic(string diagnosticId) =>
            $"https://code-cracker.github.io/diagnostics/{diagnosticId}.html";

        public static string ForDiagnostic(DiagnosticId diagnosticId) =>
            $"https://code-cracker.github.io/diagnostics/{diagnosticId.ToDiagnosticId()}.html";
    }
}
