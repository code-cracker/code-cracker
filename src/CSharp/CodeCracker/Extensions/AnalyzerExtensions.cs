﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker
{
    public static class AnalyzerExtensions
    {
        public static void RegisterSyntaxNodeAction<TLanguageKindEnum>(this AnalysisContext context, LanguageVersion languageVersion,
        Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct =>
            context.RegisterCompilationStartAction(languageVersion, compilationContext => compilationContext.RegisterSyntaxNodeAction(action, syntaxKinds));

        public static void RegisterCompilationStartAction(this AnalysisContext context, LanguageVersion languageVersion, Action<CompilationStartAnalysisContext> registrationAction) =>
            context.RegisterCompilationStartAction(compilationContext => compilationContext.RunIfCSharpVersionOrGreater(languageVersion, () => registrationAction?.Invoke(compilationContext)));

        private static void RunIfCSharpVersionOrGreater(this CompilationStartAnalysisContext context, LanguageVersion languageVersion, Action action) =>
            context.Compilation.RunIfCSharpVersionOrGreater(action, languageVersion);

        private static void RunIfCSharpVersionOrGreater(this CompilationStartAnalysisContext context, Action action, LanguageVersion languageVersion) =>
            context.Compilation.RunIfCSharpVersionOrGreater(action, languageVersion);

        private static void RunIfCSharp6OrGreater(this CompilationStartAnalysisContext context, Action action) =>
            context.Compilation.RunIfCSharp6OrGreater(action);

        private static void RunIfCSharp6OrGreater(this Compilation compilation, Action action) =>
            compilation.RunIfCSharpVersionOrGreater(action, LanguageVersion.CSharp6);

        private static void RunIfCSharpVersionOrGreater(this Compilation compilation, Action action, LanguageVersion languageVersion) =>
            (compilation as CSharpCompilation)?.LanguageVersion.RunIfCSharpVersionGreater(action, languageVersion);

        private static void RunIfCSharp6Greater(this LanguageVersion languageVersion, Action action) =>
            languageVersion.RunIfCSharpVersionGreater(action, LanguageVersion.CSharp6);

        private static void RunIfCSharpVersionGreater(this LanguageVersion languageVersion, Action action, LanguageVersion greaterOrEqualThanLanguageVersion)
        {
            if (languageVersion >= greaterOrEqualThanLanguageVersion) action?.Invoke();
        }

        public static ConditionalAccessExpressionSyntax ToConditionalAccessExpression(this MemberAccessExpressionSyntax memberAccess)
        {
            return SyntaxFactory.ConditionalAccessExpression(memberAccess.Expression, SyntaxFactory.MemberBindingExpression(memberAccess.Name));
        }

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

        public static SyntaxNode WithSameTriviaAs(this SyntaxNode target, SyntaxNode source)
        {
            if (target == null) throw new ArgumentNullException(nameof(target));
            if (source == null) throw new ArgumentNullException(nameof(source));

            return target
                .WithLeadingTrivia(source.GetLeadingTrivia())
                .WithTrailingTrivia(source.GetTrailingTrivia());
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
            return (T)node.FirstAncestorOfType(typeof(T));
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

        public static IEnumerable<TypeDeclarationSyntax> DescendantTypes(this SyntaxNode root)
        {
            return root
                .DescendantNodes(n => !(n.IsKind(SyntaxKind.MethodDeclaration)
                || n.IsKind(SyntaxKind.ConstructorDeclaration)
                || n.IsKind(SyntaxKind.DelegateDeclaration)
                || n.IsKind(SyntaxKind.DestructorDeclaration)
                || n.IsKind(SyntaxKind.EnumDeclaration)
                || n.IsKind(SyntaxKind.PropertyDeclaration)
                || n.IsKind(SyntaxKind.FieldDeclaration)
                || n.IsKind(SyntaxKind.InterfaceDeclaration)
                || n.IsKind(SyntaxKind.PropertyDeclaration)
                || n.IsKind(SyntaxKind.EventDeclaration)))
                .OfType<TypeDeclarationSyntax>();
        }

        public static IDictionary<K, V> AddRange<K, V>(this IDictionary<K, V> dictionary, IDictionary<K, V> newValues)
        {
            if (dictionary == null || newValues == null) return dictionary;
            foreach (var kv in newValues) dictionary.Add(kv);
            return dictionary;
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

        public static async Task<T> GetNodeInPositionAsync<T>(this CodeFixContext context, TextSpan span) where T : SyntaxNode
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var sourceSpan = span;
            var type = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<T>().First();
            return type;
        }
    }
}