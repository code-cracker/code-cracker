using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public interface IInconsistentAccessibilityFixInfoProvider
    {
        Task<InconsistentAccessibilityFixInfo> CreateFixInfoAsync(CodeFixContext context,
            InconsistentAccessibilitySource source);
    }
}