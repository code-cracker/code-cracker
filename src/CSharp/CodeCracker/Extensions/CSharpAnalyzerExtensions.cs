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
        public static void RegisterSyntaxNodeAction<TLanguageKindEnum>(this AnalysisContext context, LanguageVersion languageVersion,
        Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct =>
            context.RegisterCompilationStartAction(languageVersion, compilationContext => compilationContext.RegisterSyntaxNodeAction(action, syntaxKinds));

        public static void RegisterCompilationStartAction(this AnalysisContext context, LanguageVersion languageVersion, Action<CompilationStartAnalysisContext> registrationAction) =>
            context.RegisterCompilationStartAction(compilationContext => compilationContext.RunIfCSharpVersionOrGreater(languageVersion, () => registrationAction?.Invoke(compilationContext)));

        private static void RunIfCSharpVersionOrGreater(this CompilationStartAnalysisContext context, LanguageVersion languageVersion, Action action) =>
            context.Compilation.RunIfCSharpVersionOrGreater(action, languageVersion);

        private static void RunIfCSharp6OrGreater(this CompilationStartAnalysisContext context, Action action) =>
            context.Compilation.RunIfCSharp6OrGreater(action);

        private static void RunIfCSharp6OrGreater(this Compilation compilation, Action action) =>
            compilation.RunIfCSharpVersionOrGreater(action, LanguageVersion.CSharp6);

        private static void RunIfCSharpVersionOrGreater(this Compilation compilation, Action action, LanguageVersion languageVersion) =>
            (compilation as CSharpCompilation)?.LanguageVersion.RunIfCSharpVersionGreater(action, languageVersion);


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

        public static SyntaxNode GetAncestor(this SyntaxToken token, Func<SyntaxNode, bool> predicate)
        {
            return token.GetAncestor<SyntaxNode>(predicate);
        }

        public static T GetAncestor<T>(this SyntaxToken token, Func<T, bool> predicate = null)
            where T : SyntaxNode
        {
            return token.Parent != null
                ? token.Parent.FirstAncestorOrSelf(predicate)
                : default(T);
        }

#pragma warning disable CC0026 //todo: related to bug #262, remove pragma when fixed
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
#pragma warning restore CC0026
    }
}