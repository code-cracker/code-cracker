using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyObjectInitializerAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Empty Object Initializer";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Style;
        const string Description = "An object initializer without any arguments can be replaced with the standard constructor syntax.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.EmptyObjectInitializer.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            SeverityConfigurations.CurrentCS[DiagnosticId.EmptyObjectInitializer],
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Unnecessary,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.EmptyObjectInitializer));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ObjectCreationExpression);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var objectCreation = context.Node as ObjectCreationExpressionSyntax;

            if (objectCreation.Initializer != null && !objectCreation.Initializer.Expressions.Any())
            {
                var diagnostic = Diagnostic.Create(Rule, objectCreation.Initializer.OpenBraceToken.GetLocation(), "Remove the empty object initializer.");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
