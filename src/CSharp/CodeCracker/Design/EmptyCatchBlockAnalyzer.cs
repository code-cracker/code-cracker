using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using CodeCracker.Properties;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyCatchBlockAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.EmptyCatchBlockAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Design;
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.EmptyCatchBlockAnalyzer_Description), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Message = new LocalizableResourceString(nameof(Resources.EmptyCatchBlockAnalyzer_Message), Resources.ResourceManager, typeof(Resources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId.EmptyCatchBlock.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.EmptyCatchBlock));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.CatchClause);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var catchStatement = (CatchClauseSyntax)context.Node;
            if (catchStatement?.Block?.Statements.Count != 0) return;
            var diagnostic = Diagnostic.Create(Rule, catchStatement.GetLocation(), Message);
            context.ReportDiagnostic(diagnostic);
        }
    }
}