﻿using Microsoft.CodeAnalysis;
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
    }
}