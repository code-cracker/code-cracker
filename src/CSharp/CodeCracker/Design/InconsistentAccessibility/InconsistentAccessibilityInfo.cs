using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public class InconsistentAccessibilityInfo
    {
        public string CodeActionMessage { get; set; }
        public TypeSyntax TypeToChangeAccessibility { get; set; }
        public SyntaxTokenList NewAccessibilityModifiers { get; set; }
    }
}
