using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryParenthesisAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0015";
        internal const string Title = "Unnecessary Parenthesis";
        internal const string MessageFormat = "{0}";
        internal const string Category = "Syntax";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

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