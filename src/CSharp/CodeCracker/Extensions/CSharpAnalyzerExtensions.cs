using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Formatting;
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

        public static void RegisterSymbolAction(this AnalysisContext context, LanguageVersion greaterOrEqualThanLanguageVersion, Action<SymbolAnalysisContext> registrationAction, params SymbolKind[] symbolKinds) =>
            context.RegisterSymbolAction(compilationContext => compilationContext.RunIfCSharpVersionOrGreater(greaterOrEqualThanLanguageVersion, () => registrationAction?.Invoke(compilationContext)), symbolKinds);
#pragma warning disable RS1012
        private static void RunIfCSharpVersionOrGreater(this CompilationStartAnalysisContext context, LanguageVersion greaterOrEqualThanLanguageVersion, Action action) =>
            context.Compilation.RunIfCSharpVersionOrGreater(action, greaterOrEqualThanLanguageVersion);
#pragma warning restore RS1012
        private static void RunIfCSharpVersionOrGreater(this Compilation compilation, Action action, LanguageVersion greaterOrEqualThanLanguageVersion) =>
            (compilation as CSharpCompilation)?.LanguageVersion.RunIfCSharpVersionGreater(action, greaterOrEqualThanLanguageVersion);
        private static void RunIfCSharpVersionOrGreater(this SymbolAnalysisContext context, LanguageVersion greaterOrEqualThanLanguageVersion, Action action) =>
            context.Compilation.RunIfCSharpVersionOrGreater(action, greaterOrEqualThanLanguageVersion);

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

        public static MemberDeclarationSyntax FirstAncestorOrSelfThatIsAMember(this SyntaxNode node)
        {
            var currentNode = node;
            while (true)
            {
                if (currentNode == null) break;
                if (currentNode.IsAnyKind(
                    SyntaxKind.EnumDeclaration, SyntaxKind.ClassDeclaration,
                    SyntaxKind.InterfaceDeclaration, SyntaxKind.StructDeclaration,
                    SyntaxKind.ConstructorDeclaration, SyntaxKind.DestructorDeclaration,
                    SyntaxKind.MethodDeclaration, SyntaxKind.PropertyDeclaration,
                    SyntaxKind.EventDeclaration, SyntaxKind.DelegateDeclaration,
                    SyntaxKind.EventFieldDeclaration, SyntaxKind.FieldDeclaration,
                    SyntaxKind.ConversionOperatorDeclaration, SyntaxKind.OperatorDeclaration,
                    SyntaxKind.IndexerDeclaration, SyntaxKind.NamespaceDeclaration))
                    return (MemberDeclarationSyntax)currentNode;
                currentNode = currentNode.Parent;
            }
            return null;

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

        public static TNode FirstAncestorOfKind<TNode>(this SyntaxNode node, params SyntaxKind[] kinds) where TNode : SyntaxNode =>
            (TNode)FirstAncestorOfKind(node, kinds);

        public static IEnumerable<TNode> OfKind<TNode>(this IEnumerable<SyntaxNode> nodes, SyntaxKind kind) where TNode : SyntaxNode
        {
            foreach (var node in nodes)
                if (node.IsKind(kind))
                    yield return (TNode)node;
        }

        public static IEnumerable<TNode> OfKind<TNode>(this IEnumerable<SyntaxNode> nodes, params SyntaxKind[] kinds) where TNode : SyntaxNode
        {
            foreach (var node in nodes)
                if (node.IsAnyKind(kinds))
                    yield return (TNode)node;
        }

        public static IEnumerable<TNode> OfKind<TNode>(this IEnumerable<TNode> nodes, SyntaxKind kind) where TNode : SyntaxNode
        {
            foreach (var node in nodes)
                if (node.IsKind(kind))
                    yield return node;
        }

        public static IEnumerable<TNode> OfKind<TNode>(this IEnumerable<TNode> nodes, params SyntaxKind[] kinds) where TNode : SyntaxNode
        {
            foreach (var node in nodes)
                if (node.IsAnyKind(kinds))
                    yield return node;
        }

        public static StatementSyntax GetPreviousStatement(this StatementSyntax statement)
        {
            var parent = statement.Parent;
            SyntaxList<StatementSyntax> statements;
            if (parent.IsKind(SyntaxKind.Block))
            {
                var block = (BlockSyntax)parent;
                statements = block.Statements;
            }
            else if (parent.IsKind(SyntaxKind.SwitchSection))
            {
                var section = (SwitchSectionSyntax)parent;
                statements = section.Statements;
            }
            else return null;
            if (statement.Equals(statements[0])) return null;
            for (int i = 1; i < statements.Count; i++)
            {
                var someStatement = statements[i];
                if (statement.Equals(someStatement))
                    return statements[i - 1];
            }
            return null;
        }


        /// <summary>
        /// Determines whether the specified symbol is a read only field and initialized in the specified context.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="symbol">The symbol.</param>
        /// <returns>
        /// True if the symbol is a read only field that is initialized either on declaration or in all constructors of the containing type; otherwise false.
        /// </returns>
        /// <remarks>
        /// If the symbol is initialized in a block of code of the constructor that might not always be called, the symbol is considered to
        /// not be initialized for certain. For more information <seealso cref="DoesBlockContainDefiniteInitializer(SyntaxNodeAnalysisContext, ISymbol, IEnumerable{StatementSyntax})"/>
        /// </remarks>
        public static bool IsReadOnlyAndInitializedForCertain(this ISymbol symbol, SyntaxNodeAnalysisContext context)
        {
            if (symbol.Kind != SymbolKind.Field) return false;

            var field = (IFieldSymbol)symbol;
            foreach (var declaringSyntaxReference in symbol.DeclaringSyntaxReferences)
            {
                var variableDeclarator = declaringSyntaxReference.GetSyntax(context.CancellationToken) as VariableDeclaratorSyntax;

                if (variableDeclarator != null && variableDeclarator.Initializer != null && field.IsReadOnly &&
                    !variableDeclarator.Initializer.Value.IsKind(SyntaxKind.NullLiteralExpression)) return true;
            }

            foreach (var constructor in symbol.ContainingType.Constructors)
            {
                foreach (var declaringSyntaxReference in constructor.DeclaringSyntaxReferences)
                {
                    var constructorSyntax = declaringSyntaxReference.GetSyntax(context.CancellationToken) as ConstructorDeclarationSyntax;
                    if (constructorSyntax != null)
                        if (field.IsReadOnly && constructorSyntax.Body.Statements.DoesBlockContainCertainInitializer(context, symbol) == InitializerState.Initializer)
                            return true;
                }
            }

            return false;
        }

        private static InitializerState DoesBlockContainCertainInitializer(this StatementSyntax statement, SyntaxNodeAnalysisContext context, ISymbol symbol)
        {
            return new[] { statement }.DoesBlockContainCertainInitializer(context, symbol);
        }

        /// <summary>
        /// This method can be used to determine if the specified block of
        /// statements contains an initializer for the specified symbol.
        /// </summary>
        /// <param name="context">The context.</param>
        /// <param name="symbol">The symbol.</param>
        /// <param name="statements">The statements.</param>
        /// <returns>
        /// The initializer state found
        /// </returns>
        /// <remarks>
        /// Code blocks that might not always be called are:
        /// - An if or else statement.
        /// - The body of a for, while or for-each loop.
        /// - Switch statements
        ///
        /// The following exceptions are taken into account:
        /// - If both if and else statements contain a certain initialization.
        /// - If all cases in a switch contain a certain initialization (this means a default case must exist as well).
        ///
        /// Please note that this is a recursive function so we can check a block of code in an if statement for example.
        /// </remarks>
        private static InitializerState DoesBlockContainCertainInitializer(this IEnumerable<StatementSyntax> statements, SyntaxNodeAnalysisContext context, ISymbol symbol)
        {
            // Keep track of the current initializer state. This can only be None
            // or Initializer, WayToSkipInitializer will always be returned immediately.
            // Only way to go back from Initializer to None is if there is an assignment
            // to null after a previous assignment to a non-null value.
            var currentState = InitializerState.None;

            foreach (var statement in statements)
            {
                if (statement.IsKind(SyntaxKind.ReturnStatement) && currentState == InitializerState.None)
                {
                    return InitializerState.WayToSkipInitializer;
                }
                else if (statement.IsKind(SyntaxKind.Block))
                {
                    var blockResult = ((BlockSyntax)statement).Statements.DoesBlockContainCertainInitializer(context, symbol);
                    if (CanSkipInitializer(blockResult, currentState))
                        return InitializerState.WayToSkipInitializer;
                    if (blockResult == InitializerState.Initializer)
                        currentState = blockResult;
                }
                else if (statement.IsKind(SyntaxKind.UsingStatement))
                {
                    var blockResult = ((UsingStatementSyntax)statement).Statement.DoesBlockContainCertainInitializer(context, symbol);
                    if (CanSkipInitializer(blockResult, currentState))
                        return InitializerState.WayToSkipInitializer;
                    if (blockResult == InitializerState.Initializer)
                        currentState = blockResult;
                }
                else if (statement.IsKind(SyntaxKind.ExpressionStatement))
                {
                    var expression = ((ExpressionStatementSyntax)statement).Expression;
                    if (expression.IsKind(SyntaxKind.SimpleAssignmentExpression))
                    {
                        var assignmentExpression = (AssignmentExpressionSyntax)expression;
                        var identifier = assignmentExpression.Left;
                        if (identifier != null)
                        {
                            var right = assignmentExpression.Right;
                            if (right != null)
                            {
                                if (right.IsKind(SyntaxKind.NullLiteralExpression))
                                    currentState = InitializerState.None;
                                else if (symbol.Equals(context.SemanticModel.GetSymbolInfo(identifier).Symbol))
                                    currentState = InitializerState.Initializer;
                            }
                        }
                    }
                }
                else if (statement.IsKind(SyntaxKind.SwitchStatement))
                {
                    var switchStatement = (SwitchStatementSyntax)statement;
                    if (switchStatement.Sections.Any(s => s.Labels.Any(l => l.IsKind(SyntaxKind.DefaultSwitchLabel))))
                    {
                        var sectionInitializerStates = switchStatement.Sections.Select(s => s.Statements.DoesBlockContainCertainInitializer(context, symbol)).ToList();
                        if (sectionInitializerStates.All(sectionInitializerState => sectionInitializerState == InitializerState.Initializer))
                            currentState = InitializerState.Initializer;
                        else if (sectionInitializerStates.Any(sectionInitializerState => CanSkipInitializer(sectionInitializerState, currentState)))
                            return InitializerState.WayToSkipInitializer;
                    }
                }
                else if (statement.IsKind(SyntaxKind.IfStatement))
                {
                    var ifStatement = (IfStatementSyntax)statement;

                    var ifResult = ifStatement.Statement.DoesBlockContainCertainInitializer(context, symbol);
                    if (ifStatement.Else != null)
                    {
                        var elseResult = ifStatement.Else.Statement.DoesBlockContainCertainInitializer(context, symbol);

                        if (ifResult == InitializerState.Initializer && elseResult == InitializerState.Initializer)
                            currentState = InitializerState.Initializer;
                        if (CanSkipInitializer(elseResult, currentState))
                            return InitializerState.WayToSkipInitializer;
                    }
                    if (CanSkipInitializer(ifResult, currentState))
                    {
                        return InitializerState.WayToSkipInitializer;
                    }
                }
            }
            return currentState;
        }

        private static bool CanSkipInitializer(InitializerState foundState, InitializerState currentState) =>
            foundState == InitializerState.WayToSkipInitializer && currentState == InitializerState.None;

        public static TNode WithoutAllTrivia<TNode>(this TNode node) where TNode : SyntaxNode
        {
            var newNode = node.WithoutTrivia();
            var tokens = newNode.ChildTokens().ToList();
            var newTokens = tokens.ToDictionary(t => t, t => t.WithoutTrivia());
            newNode = newNode.ReplaceTokens(tokens, (o, _) => newTokens[o]);
            var nodes = newNode.ChildNodes().ToList();
            var newNodes = nodes.ToDictionary(n => n, n => n.WithoutAllTrivia());
            newNode = newNode.ReplaceNodes(nodes, (o, _) => newNodes[o]);
            newNode = newNode.WithAdditionalAnnotations(Formatter.Annotation);
            return newNode;
        }

        public static SyntaxToken WithoutTrivia(this SyntaxToken token)
        {
            var trivia = token.GetAllTrivia();
            var newToken = token.ReplaceTrivia(trivia, (o, _) => default(SyntaxTrivia));
            return newToken;
        }

        private static readonly SyntaxTokenList publicToken = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword));
        private static readonly SyntaxTokenList privateToken = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PrivateKeyword));
        private static readonly SyntaxTokenList protectedToken = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.ProtectedKeyword));
        private static readonly SyntaxTokenList protectedInternalToken = SyntaxFactory.TokenList(
            SyntaxFactory.Token(SyntaxKind.ProtectedKeyword), SyntaxFactory.Token(SyntaxKind.InternalKeyword));
        private static readonly SyntaxTokenList internalToken = SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.InternalKeyword));
        public static SyntaxTokenList GetTokens(this Accessibility accessibility)
        {
            switch (accessibility)
            {
                case Accessibility.Public:
                    return publicToken;
                case Accessibility.Private:
                    return privateToken;
                case Accessibility.Protected:
                    return protectedToken;
                case Accessibility.Internal:
                    return internalToken;
                case Accessibility.ProtectedAndInternal:
                    return protectedInternalToken;
                default:
                    throw new NotSupportedException();
            }
        }
        public static TypeDeclarationSyntax WithMembers(this TypeDeclarationSyntax typeDeclarationSyntax, SyntaxList<MemberDeclarationSyntax> members)
        {
            if (typeDeclarationSyntax is ClassDeclarationSyntax)
            {
                return (typeDeclarationSyntax as ClassDeclarationSyntax).WithMembers(members);
            }
            else if (typeDeclarationSyntax is StructDeclarationSyntax)
            {
                return (typeDeclarationSyntax as StructDeclarationSyntax).WithMembers(members);
            }
            else
            {
                throw new NotSupportedException();
            }
        }

        /// <summary>
        /// According to the C# Language Spec, item 6.4
        /// See <a href="https://github.com/ljw1004/csharpspec/blob/master/csharp/conversions.md#implicit-numeric-conversions">online</a>.
        /// </summary>
        /// <param name="from">The type to convert from</param>
        /// <param name="to">The type to convert to</param>
        public static bool HasImplicitNumericConversion(this ITypeSymbol from, ITypeSymbol to)
        {
            if (from == null || to == null) return false;
            switch (from.SpecialType)
            {
                case SpecialType.System_SByte:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_SByte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            return true;
                        default:
                            return false;
                    }
                case SpecialType.System_Byte:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_Byte:
                        case SpecialType.System_Int16:
                        case SpecialType.System_UInt16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            return true;
                        default:
                            return false;
                    }
                case SpecialType.System_Int16:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_Int16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            return true;
                        default:
                            return false;
                    }
                case SpecialType.System_UInt16:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            return true;
                        default:
                            return false;
                    }
                case SpecialType.System_Int32:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_Int32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            return true;
                        default:
                            return false;
                    }
                case SpecialType.System_UInt32:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            return true;
                        default:
                            return false;
                    }
                case SpecialType.System_Int64:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_Int64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            return true;
                        default:
                            return false;
                    }
                case SpecialType.System_UInt64:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            return true;
                        default:
                            return false;
                    }
                case SpecialType.System_Char:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_UInt16:
                        case SpecialType.System_Int32:
                        case SpecialType.System_UInt32:
                        case SpecialType.System_Int64:
                        case SpecialType.System_UInt64:
                        case SpecialType.System_Char:
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                        case SpecialType.System_Decimal:
                            return true;
                        default:
                            return false;
                    }
                case SpecialType.System_Single:
                    switch (to.SpecialType)
                    {
                        case SpecialType.System_Single:
                        case SpecialType.System_Double:
                            return true;
                        default:
                            return false;
                    }
                default:
                    return false;
            }
        }

        public static string FindAvailableIdentifierName(this SemanticModel semanticModel, int position, string baseName)
        {
            var name = baseName;
            var inscrementer = 1;
            while (semanticModel.LookupSymbols(position, name: name).Any())
                name = baseName + inscrementer++;
            return name;
        }

        public static bool IsImplementingInterface(this MemberDeclarationSyntax member, SemanticModel semanticModel) =>
            semanticModel.GetDeclaredSymbol(member).IsImplementingInterface();

        public static bool IsImplementingInterface(this ISymbol memberSymbol)
        {
            if (memberSymbol == null) return false;
            IMethodSymbol methodSymbol;
            IEventSymbol eventSymbol;
            IPropertySymbol propertySymbol;
            if ((methodSymbol = memberSymbol as IMethodSymbol) != null)
            {
                if (methodSymbol.ExplicitInterfaceImplementations.Any()) return true;
            }
            else if ((propertySymbol = memberSymbol as IPropertySymbol) != null)
            {
                if (propertySymbol.ExplicitInterfaceImplementations.Any()) return true;
            }
            else if ((eventSymbol = memberSymbol as IEventSymbol) != null)
            {
                if (eventSymbol.ExplicitInterfaceImplementations.Any()) return true;
            }
            else return false;
            var type = memberSymbol.ContainingType;
            if (type == null) return false;
            var interfaceMembersWithSameName = type.AllInterfaces.SelectMany(i => i.GetMembers(memberSymbol.Name));
            foreach (var interfaceMember in interfaceMembersWithSameName)
            {
                var implementation = type.FindImplementationForInterfaceMember(interfaceMember);
                if (implementation != null && implementation.Equals(memberSymbol)) return true;
            }
            return false;
        }
    }
}