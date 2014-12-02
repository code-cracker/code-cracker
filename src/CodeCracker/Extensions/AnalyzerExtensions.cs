using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Linq;

namespace CodeCracker
{
    public static class AnalyzerExtensions
    {
        public static void RegisterSyntaxNodeAction<TLanguageKindEnum>(this AnalysisContext context, LanguageVersion languageVersion, Action<SyntaxNodeAnalysisContext> action, params TLanguageKindEnum[] syntaxKinds) where TLanguageKindEnum : struct
        {
            context.RegisterCompilationStartAction(LanguageVersion.CSharp6, compilationContext => compilationContext.RegisterSyntaxNodeAction(action, syntaxKinds));
        }

        public static void RegisterCompilationStartAction(this AnalysisContext context, LanguageVersion languageVersion, Action<CompilationStartAnalysisContext> registrationAction)
        {
            context.RegisterCompilationStartAction(compilationContext => compilationContext.RunIfCSharp6OrGreater(() => registrationAction(compilationContext)));
        }

        private static void RunIfCSharp6OrGreater(this CompilationStartAnalysisContext context, Action action)
        {
            context.Compilation.RunIfCSharp6OrGreater(action);
        }

        private static void RunIfCSharp6OrGreater(this Compilation compilation, Action action)
        {
            var cSharpCompilation = compilation as CSharpCompilation;
            if (cSharpCompilation == null) return;
            cSharpCompilation.LanguageVersion.RunIfCSharp6OrGreater(action);
        }

        private static void RunIfCSharp6OrGreater(this LanguageVersion languageVersion, Action action)
        {
            if (languageVersion >= LanguageVersion.CSharp6) action();
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
            if (source == null) throw new ArgumentNullException(nameof(target));

            return target
                .WithLeadingTrivia(source.GetLeadingTrivia())
                .WithTrailingTrivia(source.GetTrailingTrivia());
        }
    }
}
