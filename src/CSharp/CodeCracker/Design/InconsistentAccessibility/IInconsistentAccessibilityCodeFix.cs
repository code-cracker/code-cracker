using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public interface IInconsistentAccessibilityCodeFix
    {
        Task FixAsync(CodeFixContext context, Diagnostic diagnostic, InconsistentAccessibilitySource source, InconsistentAccessibilityFixInfo fixInfo);
    }
}