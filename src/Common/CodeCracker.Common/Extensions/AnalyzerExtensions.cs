using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeCracker
{
    public static partial class AnalyzerExtensions
    {
        public static SyntaxNode WithSameTriviaAs(this SyntaxNode target, SyntaxNode source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));

            return target
                .WithLeadingTrivia(source.GetLeadingTrivia())
                .WithTrailingTrivia(source.GetTrailingTrivia());
        }

        public static bool IsAnyKind(this SyntaxNode node, params SyntaxKind[] kinds)
        {
            foreach (var kind in kinds)
            {
                if (node.IsKind(kind)) return true;
            }
            return false;
        }

        public static bool IsAnyKind(this SymbolDisplayPart displayPart, params SymbolDisplayPartKind[] kinds)
        {
            foreach (var kind in kinds)
            {
                if (displayPart.Kind == kind) return true;
            }
            return false;
        }

        public static StatementSyntax FirstAncestorOrSelfThatIsAStatement(this SyntaxNode node)
        {
            var currentNode = node;
            while (true)
            {
                if (currentNode == null) break;
                if (currentNode.IsAnyKind(SyntaxKind.Block, SyntaxKind.BreakStatement,
                    SyntaxKind.CheckedStatement, SyntaxKind.ContinueStatement,
                    SyntaxKind.DoStatement, SyntaxKind.EmptyStatement,
                    SyntaxKind.ExpressionStatement, SyntaxKind.FixedKeyword,
                    SyntaxKind.ForEachKeyword, SyntaxKind.ForStatement,
                    SyntaxKind.GotoStatement, SyntaxKind.IfStatement,
                    SyntaxKind.LabeledStatement, SyntaxKind.LocalDeclarationStatement,
                    SyntaxKind.LockStatement, SyntaxKind.ReturnStatement,
                    SyntaxKind.SwitchStatement, SyntaxKind.ThrowStatement,
                    SyntaxKind.TryStatement, SyntaxKind.UnsafeStatement,
                    SyntaxKind.UsingStatement, SyntaxKind.WhileStatement,
                    SyntaxKind.YieldBreakStatement, SyntaxKind.YieldReturnStatement))
                    return (StatementSyntax)currentNode;
                currentNode = currentNode.Parent;
            }
            return null;
        }

        public static T FirstAncestorOrSelfOfType<T>(this SyntaxNode node) where T : SyntaxNode =>
            (T)node.FirstAncestorOrSelfOfType(typeof(T));

        public static SyntaxNode FirstAncestorOrSelfOfType(this SyntaxNode node, params Type[] types)
        {
            var currentNode = node;
            while (true)
            {
                if (currentNode == null) break;
                foreach (var type in types)
                {
                    if (currentNode.GetType() == type) return currentNode;
                }
                currentNode = currentNode.Parent;
            }
            return null;
        }

        public static T FirstAncestorOfType<T>(this SyntaxNode node) where T : SyntaxNode =>
            (T)node.FirstAncestorOfType(typeof(T));

        public static SyntaxNode FirstAncestorOfType(this SyntaxNode node, params Type[] types)
        {
            var currentNode = node;
            while (true)
            {
                var parent = currentNode.Parent;
                if (parent == null) break;
                foreach (var type in types)
                {
                    if (parent.GetType() == type) return parent;
                }
                currentNode = parent;
            }
            return null;
        }

        public static IList<IMethodSymbol> GetAllMethodsIncludingFromInnerTypes(this INamedTypeSymbol typeSymbol)
        {
            var methods = typeSymbol.GetMembers().OfType<IMethodSymbol>().ToList();
            var innerTypes = typeSymbol.GetMembers().OfType<INamedTypeSymbol>();
            foreach (var innerType in innerTypes)
            {
                methods.AddRange(innerType.GetAllMethodsIncludingFromInnerTypes());
            }
            return methods;
        }

        public static IEnumerable<INamedTypeSymbol> AllBaseTypesAndSelf(this INamedTypeSymbol typeSymbol)
        {
            yield return typeSymbol;
            foreach (var b in AllBaseTypes(typeSymbol))
                yield return b;
        }

        public static IEnumerable<INamedTypeSymbol> AllBaseTypes(this INamedTypeSymbol typeSymbol)
        {
            while (typeSymbol.BaseType != null)
            {
                yield return typeSymbol.BaseType;
                typeSymbol = typeSymbol.BaseType;
            }
        }

        public static bool HasAttributeOnAncestorOrSelf(this SyntaxNode node, string attributeName)
        {
            var csharpNode = node as CSharpSyntaxNode;
            if (csharpNode != null)
                return csharpNode.HasAttributeOnAncestorOrSelf(attributeName);
            var vbNode = node as Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode;
            if (vbNode != null)
                return vbNode.HasAttributeOnAncestorOrSelf(attributeName);
            return false;
        }

        public static bool HasAttributeOnAncestorOrSelf(this SyntaxNode node, params string[] attributeNames)
        {
            var csharpNode = node as CSharpSyntaxNode;
            if (csharpNode != null)
                foreach (var attributeName in attributeNames)
                    if (csharpNode.HasAttributeOnAncestorOrSelf(attributeName)) return true;
            var vbNode = node as Microsoft.CodeAnalysis.VisualBasic.VisualBasicSyntaxNode;
            if (vbNode != null)
                foreach (var attributeName in attributeNames)
                    if (vbNode.HasAttributeOnAncestorOrSelf(attributeName)) return true;
            return false;
        }

        public static bool HasAttributeOnAncestorOrSelf(this CSharpSyntaxNode node, string attributeName)
        {
            var parentMethod = (BaseMethodDeclarationSyntax)node.FirstAncestorOrSelfOfType(typeof(MethodDeclarationSyntax), typeof(ConstructorDeclarationSyntax));
            if (parentMethod?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var type = (TypeDeclarationSyntax)node.FirstAncestorOrSelfOfType(typeof(ClassDeclarationSyntax), typeof(StructDeclarationSyntax));
            while (type != null)
            {
                if (type.AttributeLists.HasAttribute(attributeName))
                    return true;
                type = (TypeDeclarationSyntax)type.FirstAncestorOfType(typeof(ClassDeclarationSyntax), typeof(StructDeclarationSyntax));
            }
            var property = node.FirstAncestorOrSelfOfType<PropertyDeclarationSyntax>();
            if (property?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var accessor = node.FirstAncestorOrSelfOfType<AccessorDeclarationSyntax>();
            if (accessor?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var anInterface = node.FirstAncestorOrSelfOfType<InterfaceDeclarationSyntax>();
            if (anInterface?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var anEvent = node.FirstAncestorOrSelfOfType<EventDeclarationSyntax>();
            if (anEvent?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var anEnum = node.FirstAncestorOrSelfOfType<EnumDeclarationSyntax>();
            if (anEnum?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var field = node.FirstAncestorOrSelfOfType<FieldDeclarationSyntax>();
            if (field?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var eventField = node.FirstAncestorOrSelfOfType<EventFieldDeclarationSyntax>();
            if (eventField?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var parameter = node as ParameterSyntax;
            if (parameter?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            var aDelegate = node as DelegateDeclarationSyntax;
            if (aDelegate?.AttributeLists.HasAttribute(attributeName) ?? false)
                return true;
            return false;
        }

        public static bool HasAttribute(this SyntaxList<AttributeListSyntax> attributeLists, string attributeName) =>
            attributeLists.SelectMany(a => a.Attributes).Any(a => a.Name.ToString().EndsWith(attributeName, StringComparison.OrdinalIgnoreCase));

        public static NameSyntax ToNameSyntax(this INamespaceSymbol namespaceSymbol) =>
            ToNameSyntax(namespaceSymbol.ToDisplayString().Split('.'));

        private static NameSyntax ToNameSyntax(IEnumerable<string> names)
        {
            var count = names.Count();
            if (count == 1)
                return SyntaxFactory.IdentifierName(names.First());
            return SyntaxFactory.QualifiedName(
                ToNameSyntax(names.Take(count - 1)),
                ToNameSyntax(names.Skip(count - 1)) as IdentifierNameSyntax
            );
        }

        public static TypeSyntax FindTypeInParametersList(this SeparatedSyntaxList<ParameterSyntax> parameterList, string typeName)
        {
            TypeSyntax result = null;
            var lastIdentifierOfTypeName = GetLastIdentifierIfQualiedTypeName(typeName);
            foreach (var parameter in parameterList)
            {
                var valueText = GetLastIdentifierValueText(parameter.Type);

                if (!string.IsNullOrEmpty(valueText))
                {
                    if (string.Equals(valueText, lastIdentifierOfTypeName, StringComparison.Ordinal))
                    {
                        result = parameter.Type;
                        break;
                    }
                }
            }

            return result;
        }

        private static string GetLastIdentifierIfQualiedTypeName(string typeName)
        {
            var result = typeName;

            var parameterTypeDotIndex = typeName.LastIndexOf('.');
            if (parameterTypeDotIndex > 0)
            {
                result = typeName.Substring(parameterTypeDotIndex + 1);
            }

            return result;
        }

        private static string GetLastIdentifierValueText(CSharpSyntaxNode node)
        {
            var result = string.Empty;
            switch (node.Kind())
            {
                case SyntaxKind.IdentifierName:
                    result = ((IdentifierNameSyntax)node).Identifier.ValueText;
                    break;
                case SyntaxKind.QualifiedName:
                    result = GetLastIdentifierValueText(((QualifiedNameSyntax)node).Right);
                    break;
                case SyntaxKind.GenericName:
                    var genericNameSyntax = ((GenericNameSyntax)node);
                    result = $"{genericNameSyntax.Identifier.ValueText}{genericNameSyntax.TypeArgumentList.ToString()}";
                    break;
                case SyntaxKind.AliasQualifiedName:
                    result = ((AliasQualifiedNameSyntax)node).Name.Identifier.ValueText;
                    break;
            }

            return result;
        }

        public static SyntaxToken GetIdentifier(this BaseMethodDeclarationSyntax method)
        {
            var result = default(SyntaxToken);

            switch(method.Kind())
            {
                case SyntaxKind.MethodDeclaration:
                    result = ((MethodDeclarationSyntax)method).Identifier;
                    break;
                case SyntaxKind.ConstructorDeclaration:
                    result = ((ConstructorDeclarationSyntax)method).Identifier;
                    break;
                case SyntaxKind.DestructorDeclaration:
                    result = ((DestructorDeclarationSyntax)method).Identifier;
                    break;
            }

            return result;
        }

        public static MemberDeclarationSyntax WithModifiers(this MemberDeclarationSyntax declaration, SyntaxTokenList newModifiers)
        {
            var result = declaration;

            switch (declaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                    result = ((ClassDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.StructDeclaration:
                    result = ((StructDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.InterfaceDeclaration:
                    result = ((InterfaceDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.EnumDeclaration:
                    result = ((EnumDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.DelegateDeclaration:
                    result = ((DelegateDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.FieldDeclaration:
                    result = ((FieldDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.EventFieldDeclaration:
                    result = ((EventFieldDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.MethodDeclaration:
                    result = ((MethodDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.OperatorDeclaration:
                    result = ((OperatorDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.ConversionOperatorDeclaration:
                    result = ((ConversionOperatorDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.ConstructorDeclaration:
                    result = ((ConstructorDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.DestructorDeclaration:
                    result = ((DestructorDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.PropertyDeclaration:
                    result = ((PropertyDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.IndexerDeclaration:
                    result = ((IndexerDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
                case SyntaxKind.EventDeclaration:
                    result = ((EventDeclarationSyntax)declaration).WithModifiers(newModifiers);
                    break;
            }

            return result;
        }

        public static SyntaxTokenList GetModifiers(this MemberDeclarationSyntax memberDeclaration)
        {
            var result = default(SyntaxTokenList);

            switch (memberDeclaration.Kind())
            {
                case SyntaxKind.ClassDeclaration:
                case SyntaxKind.StructDeclaration:
                case SyntaxKind.InterfaceDeclaration:
                case SyntaxKind.EnumDeclaration:
                    result = ((BaseTypeDeclarationSyntax)memberDeclaration).Modifiers;
                    break;
                case SyntaxKind.DelegateDeclaration:
                    result = ((DelegateDeclarationSyntax)memberDeclaration).Modifiers;
                    break;
                case SyntaxKind.FieldDeclaration:
                case SyntaxKind.EventFieldDeclaration:
                    result = ((BaseFieldDeclarationSyntax)memberDeclaration).Modifiers;
                    break;
                case SyntaxKind.MethodDeclaration:
                case SyntaxKind.OperatorDeclaration:
                case SyntaxKind.ConversionOperatorDeclaration:
                case SyntaxKind.ConstructorDeclaration:
                case SyntaxKind.DestructorDeclaration:
                    result = ((BaseMethodDeclarationSyntax)memberDeclaration).Modifiers;
                    break;
                case SyntaxKind.PropertyDeclaration:
                case SyntaxKind.IndexerDeclaration:
                case SyntaxKind.EventDeclaration:
                    result = ((BasePropertyDeclarationSyntax)memberDeclaration).Modifiers;
                    break;
            }

            return result;
        }

        public static SyntaxTokenList CloneAccessibilityModifiers(this BaseMethodDeclarationSyntax method)
        {
            var modifiers = method.Modifiers;
            if (method.Parent.IsKind(SyntaxKind.InterfaceDeclaration))
            {
                modifiers = ((InterfaceDeclarationSyntax)method.Parent).Modifiers;
            }

            return modifiers.CloneAccessibilityModifiers();
        }

        public static SyntaxTokenList CloneAccessibilityModifiers(this SyntaxTokenList modifiers)
        {
            var accessibilityModifiers = modifiers.Where(token => token.IsKind(SyntaxKind.PublicKeyword) || token.IsKind(SyntaxKind.ProtectedKeyword) || token.IsKind(SyntaxKind.InternalKeyword) || token.IsKind(SyntaxKind.PrivateKeyword)).Select(token => SyntaxFactory.Token(token.Kind()));

            return SyntaxFactory.TokenList(EnsureProtectedBeforeInternal(accessibilityModifiers));
        }

        private static IEnumerable<SyntaxToken> EnsureProtectedBeforeInternal(IEnumerable<SyntaxToken> modifiers) => modifiers.OrderByDescending(token => token.RawKind);
    }
}