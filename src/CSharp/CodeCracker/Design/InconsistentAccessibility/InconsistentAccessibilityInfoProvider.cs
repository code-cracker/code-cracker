using Microsoft.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public interface InconsistentAccessibilityInfoProvider
    {
        InconsistentAccessibilityInfo GetInconsistentAccessibilityInfo(SyntaxNode syntaxRoot, Diagnostic diagnostic);
    }
}
