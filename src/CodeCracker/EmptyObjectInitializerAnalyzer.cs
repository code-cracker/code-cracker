using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyObjectInitializerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0005";
        internal const string Title = "Empty Object Initializer";
        internal const string MessageFormat = "{0}";
        internal const string Category = "Syntax";
        const string Description = "An empty object initializer doesn't add any information and only clutter the code.\r\n"
            + "If there is no member to initialize, prefer using the standard constructor syntax.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Unnecessary,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ObjectCreationExpression);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var objectCreation = context.Node as ObjectCreationExpressionSyntax;

            if (objectCreation.Initializer != null && !objectCreation.Initializer.Expressions.Any())
            {
                var diagnostic = Diagnostic.Create(Rule, objectCreation.Initializer.OpenBraceToken.GetLocation(), "Remove empty object initializer.");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}