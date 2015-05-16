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

        private static void RunIfCSharpVersionOrGreater(this CompilationStartAnalysisContext context, LanguageVersion greaterOrEqualThanLanguageVersion, Action action) =>
            context.Compilation.RunIfCSharpVersionOrGreater(action, greaterOrEqualThanLanguageVersion);

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

        private static void RunIfCSharpVersionLower(this CompilationStartAnalysisContext context, LanguageVersion lowerThanLanguageVersion, Action action) =>
            context.Compilation.RunIfCSharpVersionLower(action, lowerThanLanguageVersion);

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
    }
}