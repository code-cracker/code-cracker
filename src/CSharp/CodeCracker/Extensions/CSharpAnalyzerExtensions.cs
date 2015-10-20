using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CodeCracker
{
    public static class CSharpAnalyzerExtensions
    {
        public static void RegisterSyntaxNodeAction<TLanguageKindEnum>(this AnalysisContext context, LanguageVersion greaterOrEqualThanLanguageVersion,
        Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct =>
            context.RegisterCompilationStartAction(greaterOrEqualThanLanguageVersion, compilationContext => compilationContext.RegisterSyntaxNodeAction(action, syntaxKinds));

        public static void RegisterCompilationStartAction(this AnalysisContext context, LanguageVersion greaterOrEqualThanLanguageVersion, Action<CompilationStartAnalysisContext> registrationAction) =>
            context.RegisterCompilationStartAction(compilationContext => compilationContext.RunIfCSharpVersionOrGreater(greaterOrEqualThanLanguageVersion, () => registrationAction?.Invoke(compilationContext)));
#pragma warning disable RS1012
        private static void RunIfCSharpVersionOrGreater(this CompilationStartAnalysisContext context, LanguageVersion greaterOrEqualThanLanguageVersion, Action action) =>
            context.Compilation.RunIfCSharpVersionOrGreater(action, greaterOrEqualThanLanguageVersion);
#pragma warning restore RS1012
        private static void RunIfCSharpVersionOrGreater(this Compilation compilation, Action action, LanguageVersion greaterOrEqualThanLanguageVersion) =>
            (compilation as CSharpCompilation)?.LanguageVersion.RunIfCSharpVersionGreater(action, greaterOrEqualThanLanguageVersion);

        private static void RunIfCSharpVersionGreater(this LanguageVersion languageVersion, Action action, LanguageVersion greaterOrEqualThanLanguageVersion)
        {
            if (languageVersion >= greaterOrEqualThanLanguageVersion) action?.Invoke();
        }

        public static void RegisterSyntaxNodeActionForVersionLower<TLanguageKindEnum>(this AnalysisContext context, LanguageVersion lowerThanLanguageVersion,
        Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct =>
            context.RegisterCompilationStartActionForVersionLower(lowerThanLanguageVersion, compilationContext => compilationContext.RegisterSyntaxNodeAction(action, syntaxKinds));

        public static void RegisterCompilationStartActionForVersionLower(this AnalysisContext context, LanguageVersion lowerThanLanguageVersion, Action<CompilationStartAnalysisContext> registrationAction) =>
            context.RegisterCompilationStartAction(compilationContext => compilationContext.RunIfCSharpVersionLower(lowerThanLanguageVersion, () => registrationAction?.Invoke(compilationContext)));
#pragma warning disable RS1012
        private static void RunIfCSharpVersionLower(this CompilationStartAnalysisContext context, LanguageVersion lowerThanLanguageVersion, Action action) =>
            context.Compilation.RunIfCSharpVersionLower(action, lowerThanLanguageVersion);
#pragma warning restore RS1012
        private static void RunIfCSharpVersionLower(this Compilation compilation, Action action, LanguageVersion lowerThanLanguageVersion) =>
            (compilation as CSharpCompilation)?.LanguageVersion.RunIfCSharpVersionLower(action, lowerThanLanguageVersion);

        private static void RunIfCSharpVersionLower(this LanguageVersion languageVersion, Action action, LanguageVersion lowerThanLanguageVersion)
        {
            if (languageVersion < lowerThanLanguageVersion) action?.Invoke();
        }

        public static ConditionalAccessExpressionSyntax ToConditionalAccessExpression(this MemberAccessExpressionSyntax memberAccess) =>
            SyntaxFactory.ConditionalAccessExpression(memberAccess.Expression, SyntaxFactory.MemberBindingExpression(memberAccess.Name));

        public static StatementSyntax GetSingleStatementFromPossibleBlock(this StatementSyntax statement)
        {
            var block = statement as BlockSyntax;
            if (block != null)
            {
                if (block.Statements.Count != 1) return null;
                return block.Statements.Single();
            }
            else
            {
                return statement;
            }
        }

        public static bool IsEmbeddedStatementOwner(this SyntaxNode node)
        {
            return node is IfStatementSyntax ||
                   node is ElseClauseSyntax ||
                   node is ForStatementSyntax ||
                   node is ForEachStatementSyntax ||
                   node is WhileStatementSyntax ||
                   node is UsingStatementSyntax ||
                   node is DoStatementSyntax ||
                   node is LockStatementSyntax ||
                   node is FixedStatementSyntax;
        }

        public static IEnumerable<TypeDeclarationSyntax> DescendantTypes(this SyntaxNode root)
        {
            return root
                .DescendantNodes(n => !(n.IsKind(
                    SyntaxKind.MethodDeclaration,
                    SyntaxKind.ConstructorDeclaration,
                    SyntaxKind.DelegateDeclaration,
                    SyntaxKind.DestructorDeclaration,
                    SyntaxKind.EnumDeclaration,
                    SyntaxKind.PropertyDeclaration,
                    SyntaxKind.FieldDeclaration,
                    SyntaxKind.InterfaceDeclaration,
                    SyntaxKind.PropertyDeclaration,
                    SyntaxKind.EventDeclaration)))
                .OfType<TypeDeclarationSyntax>();
        }

        public static T GetAncestor<T>(this SyntaxToken token, Func<T, bool> predicate = null)
            where T : SyntaxNode => token.Parent?.FirstAncestorOrSelf(predicate);

        public static bool IsKind(this SyntaxToken token, params SyntaxKind[] kinds)
        {
            foreach (var kind in kinds)
                if (Microsoft.CodeAnalysis.CSharpExtensions.IsKind(token, kind)) return true;
            return false;
        }

        public static bool IsKind(this SyntaxTrivia trivia, params SyntaxKind[] kinds)
        {
            foreach (var kind in kinds)
                if (Microsoft.CodeAnalysis.CSharpExtensions.IsKind(trivia, kind)) return true;
            return false;
        }

        public static bool IsKind(this SyntaxNode node, params SyntaxKind[] kinds)
        {
            foreach (var kind in kinds)
                if (Microsoft.CodeAnalysis.CSharpExtensions.IsKind(node, kind)) return true;
            return false;
        }

        public static bool IsKind(this SyntaxNodeOrToken nodeOrToken, params SyntaxKind[] kinds)
        {
            foreach (var kind in kinds)
                if (Microsoft.CodeAnalysis.CSharpExtensions.IsKind(nodeOrToken, kind)) return true;
            return false;
        }

        public static bool IsNotKind(this SyntaxNode node, params SyntaxKind[] kinds) => !node.IsKind(kinds);

        public static bool Any(this SyntaxTokenList list, SyntaxKind kind1, SyntaxKind kind2) =>
            list.IndexOf(kind1) >= 0 || list.IndexOf(kind2) >= 0;

        public static bool Any(this SyntaxTokenList list, SyntaxKind kind1, SyntaxKind kind2, params SyntaxKind[] kinds)
        {
            if (list.Any(kind1, kind2)) return true;
            for (int i = 0; i < kinds.Length; i++)
                if (list.IndexOf(kinds[i]) >= 0) return true;
            return false;
        }

        public static bool IsAnyKind(this SyntaxNode node, params SyntaxKind[] kinds)
        {
            foreach (var kind in kinds)
            {
                if (node.IsKind(kind)) return true;
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

        public static bool HasAttributeOnAncestorOrSelf(this SyntaxNode node, string attributeName)
        {
            var csharpNode = node as CSharpSyntaxNode;
            if (csharpNode == null) throw new Exception("Node is not a C# node");
            return csharpNode.HasAttributeOnAncestorOrSelf(attributeName);
        }

        public static bool HasAttributeOnAncestorOrSelf(this SyntaxNode node, params string[] attributeNames)
        {
            var csharpNode = node as CSharpSyntaxNode;
            if (csharpNode == null) throw new Exception("Node is not a C# node");
            foreach (var attributeName in attributeNames)
                if (csharpNode.HasAttributeOnAncestorOrSelf(attributeName)) return true;
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

        public static bool HasAnyAttribute(this SyntaxList<AttributeListSyntax> attributeLists, string[] attributeNames) =>
            attributeLists.SelectMany(a => a.Attributes).Select(a => a.Name.ToString()).Any(name => attributeNames.Any(attributeName =>
            name.EndsWith(attributeName, StringComparison.OrdinalIgnoreCase)
            || name.EndsWith($"{attributeName}Attribute", StringComparison.OrdinalIgnoreCase)));

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
            var lastIdentifierOfTypeName = typeName.GetLastIdentifierIfQualiedTypeName();
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

            switch (method.Kind())
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

            return SyntaxFactory.TokenList(accessibilityModifiers.EnsureProtectedBeforeInternal());
        }

        public static SyntaxNode FirstAncestorOfKind(this SyntaxNode node, params SyntaxKind[] kinds)
        {
            var currentNode = node;
            while (true)
            {
                var parent = currentNode.Parent;
                if (parent == null) break;
                if (parent.IsAnyKind(kinds)) return parent;
                currentNode = parent;
            }
            return null;
        }
    }
}