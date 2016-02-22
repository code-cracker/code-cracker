using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeCracker
{
    public static partial class AnalyzerExtensions
    {
        public static bool IsName(this SymbolDisplayPart displayPart) =>
            displayPart.IsAnyKind(SymbolDisplayPartKind.ClassName, SymbolDisplayPartKind.DelegateName,
                                  SymbolDisplayPartKind.EnumName, SymbolDisplayPartKind.EventName,
                                  SymbolDisplayPartKind.FieldName, SymbolDisplayPartKind.InterfaceName,
                                  SymbolDisplayPartKind.LocalName, SymbolDisplayPartKind.MethodName,
                                  SymbolDisplayPartKind.NamespaceName, SymbolDisplayPartKind.ParameterName,
                                  SymbolDisplayPartKind.PropertyName, SymbolDisplayPartKind.StructName);

        public static SyntaxNode WithSameTriviaAs(this SyntaxNode target, SyntaxNode source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));

            return target
                .WithLeadingTrivia(source.GetLeadingTrivia())
                .WithTrailingTrivia(source.GetTrailingTrivia());
        }

        public static bool IsAnyKind(this SymbolDisplayPart displayPart, params SymbolDisplayPartKind[] kinds)
        {
            foreach (var kind in kinds)
            {
                if (displayPart.Kind == kind) return true;
            }
            return false;
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

        public static T FirstAncestorOfType<T>(this SyntaxNode node) where T : SyntaxNode
        {
            var currentNode = node;
            while (true)
            {
                var parent = currentNode.Parent;
                if (parent == null) break;
                var tParent = parent as T;
                if (tParent != null) return tParent;
                currentNode = parent;
            }
            return null;
        }

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

        public static string GetLastIdentifierIfQualiedTypeName(this string typeName)
        {
            var result = typeName;

            var parameterTypeDotIndex = typeName.LastIndexOf('.');
            if (parameterTypeDotIndex > 0)
            {
                result = typeName.Substring(parameterTypeDotIndex + 1);
            }

            return result;
        }
        public static IEnumerable<SyntaxToken> EnsureProtectedBeforeInternal(this IEnumerable<SyntaxToken> modifiers) => modifiers.OrderByDescending(token => token.RawKind);

        public static string GetFullName(this ISymbol symbol, bool addGlobal = true)
        {
            var fullName = symbol.Name;
            var containingSymbol = symbol.ContainingSymbol;
            while (!(containingSymbol is INamespaceSymbol))
            {
                fullName = $"{containingSymbol.Name}.{fullName}";
                containingSymbol = containingSymbol.ContainingSymbol;
            }
            if (!((INamespaceSymbol)containingSymbol).IsGlobalNamespace)
                fullName = $"{containingSymbol.ToString()}.{fullName}";
            if (addGlobal)
                fullName = $"global::{fullName}";
            return fullName;
        }

        public static IEnumerable<INamedTypeSymbol> GetAllContainingTypes(this ISymbol symbol)
        {
            while (symbol.ContainingType != null)
            {
                yield return symbol.ContainingType;
                symbol = symbol.ContainingType;
            }
        }

        public static Accessibility GetMinimumCommonAccessibility(this Accessibility accessibility, Accessibility otherAccessibility)
        {
            if (accessibility == otherAccessibility || otherAccessibility == Accessibility.Private) return accessibility;
            if (otherAccessibility == Accessibility.Public) return Accessibility.Public;
            switch (accessibility)
            {
                case Accessibility.Private:
                    return otherAccessibility;
                case Accessibility.ProtectedAndInternal:
                case Accessibility.Protected:
                case Accessibility.Internal:
                    return Accessibility.ProtectedAndInternal;
                case Accessibility.Public:
                    return Accessibility.Public;
                default:
                    throw new NotSupportedException();
            }
        }

        public static bool IsPrimitive(this ITypeSymbol typeSymbol)
        {
            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Char:
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}