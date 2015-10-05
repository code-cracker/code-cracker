using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConsoleWriteLineAnalyzer : DiagnosticAnalyzer
    {
        internal const string Category = SupportedCategories.Style;
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ConsoleWriteLineAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ConsoleWriteLineAnalyzer_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ConsoleWriteLineAnalyzer_Description), Resources.ResourceManager, typeof(Resources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ConsoleWriteLine.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ConsoleWriteLine));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeWriteLineInvocation, SyntaxKind.InvocationExpression);

        private static void AnalyzeWriteLineInvocation(SyntaxNodeAnalysisContext context) =>
            StringFormatAnalyzer.AnalyzeFormatInvocation(context, "WriteLine", "System.Console.WriteLine(string, object", "System.Console.WriteLine(string, params object[])", Rule);
    }
}