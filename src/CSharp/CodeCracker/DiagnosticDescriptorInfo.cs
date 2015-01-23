using Microsoft.CodeAnalysis;
using System.Collections.Generic;
using System.Linq;

namespace CodeCracker
{
    public class DiagnosticDescriptorInfo
    {
        public DiagnosticId Id = DiagnosticId.None;
        public string Title;
        public string Message;
        public string Category;
        public string Description;
        public DiagnosticSeverity Severity = DiagnosticSeverity.Info;
        public bool IsEnableByDefault = true;
        public IList<string> CustomTags = new List<string>();
        public DiagnosticDescriptor ToDiagnosticDescriptor()
        {
            if (Id == DiagnosticId.None || string.IsNullOrWhiteSpace(Title)
            || string.IsNullOrWhiteSpace(Message) || string.IsNullOrWhiteSpace(Category)
            || string.IsNullOrWhiteSpace(Description))
                throw new System.InvalidOperationException("All values but custom tags are required.");
            var stringId = Id.ToDiagnosticId();
            var descriptor = new DiagnosticDescriptor(
                stringId,
                Title,
                Message,
                Category,
                Severity,
                IsEnableByDefault,
                Description,
                LinkForDiagnostic(stringId),
                CustomTags.ToArray());
            return descriptor;
        }
        public static string LinkForDiagnostic(DiagnosticId diagnosticId) => LinkForDiagnostic(diagnosticId.ToDiagnosticId());
        public static string LinkForDiagnostic(string diagnosticId) => $"https://code-cracker.github.io/diagnostics/{diagnosticId}.html";
    }
}