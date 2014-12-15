using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryParenthesisAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0015";
        internal const string Title = "Unnecessary Parenthesis";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Style;
        const string Description = "There is no need to specify that the no-parameter constructor is used with "
            + " an initializer as it is implicit";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            customTags: WellKnownDiagnosticTags.Unnecessary,
            isEnabledByDefault: true,
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
            if (objectCreation.Initializer != null && objectCreation.ArgumentList != null && !objectCreation.ArgumentList.Arguments.Any())
            {
                var diagnostic = Diagnostic.Create(Rule, objectCreation.ArgumentList.OpenParenToken.GetLocation(), "Remove unnecessary parenthesis.");
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}