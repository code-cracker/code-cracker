using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public class InconsistentAccessibilityFixInfo
    {
        public InconsistentAccessibilityFixInfo(TypeSyntax type, SyntaxTokenList modifiers)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }
            Type = type;
            Modifiers = modifiers;
        }

        public TypeSyntax Type { get; private set; }
        public SyntaxTokenList Modifiers { get; private set; }
    }
}