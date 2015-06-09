using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public class InconsistentAccessibilityInfo
    {
        public string CodeActionMessage { get; set; }
        public string InconsistentAccessibilityTypeName { get; set; }
        public SyntaxTokenList NewAccessibilityModifiers { get; set; }
    }
}
