using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseStringEmptyAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Consider use 'String.Empty'";
        internal const string MessageFormat = "Use 'String.Empty' instead of \"\"";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Consider user 'String.Empty' instead of \"\"";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.UseStringEmpty.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.UseStringEmpty));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNodeVariableDeclaration, SyntaxKind.StringLiteralExpression);

        private static void AnalyzeNodeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var literal = context.Node as LiteralExpressionSyntax;
            if (literal.ToString() != "\"\"" || literal.Ancestors().OfType<AttributeArgumentSyntax>().Any())
                return;
            var diagnostic = Diagnostic.Create(Rule, literal.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}