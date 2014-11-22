using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;

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
    }
}
