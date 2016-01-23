using System;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Design.InconsistentAccessibility
{
    public class InconsistentAccessibilitySource
    {
        public static readonly InconsistentAccessibilitySource Invalid =
            new InconsistentAccessibilitySource(string.Empty, SyntaxFactory.ParseName("_"),
                SyntaxTokenList.Create(SyntaxFactory.Token(SyntaxKind.BadToken)));

        public InconsistentAccessibilitySource(string codeActionMessage, TypeSyntax typeToChangeAccessibility,
            SyntaxTokenList modifiers)
        {
            if (codeActionMessage == null)
            {
                throw new ArgumentNullException(nameof(codeActionMessage));
            }
            if (typeToChangeAccessibility == null)
            {
                throw new ArgumentNullException(nameof(typeToChangeAccessibility));
            }
            CodeActionMessage = codeActionMessage;
            TypeToChangeAccessibility = typeToChangeAccessibility;
            Modifiers = modifiers;
        }

        public string CodeActionMessage { get; private set; }
        public TypeSyntax TypeToChangeAccessibility { get; private set; }
        public SyntaxTokenList Modifiers { get; private set; }

        public bool TypeToChangeFound()
        {
            return TypeToChangeAccessibility != null;
        }
    }
}
