using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseEmptyStringAnalyzer : DiagnosticAnalyzer
    {
        private const string EmptyString = "\"\"";
        private const string InsteadStringEmpty = " instead of 'string.Empty'";
        internal const string Title = "Consider use " + EmptyString;

        internal const string MessageFormat = "Use " + EmptyString + InsteadStringEmpty;
        internal const string Category = SupportedCategories.Style;
        const string Description = "Consider using " + EmptyString + InsteadStringEmpty;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.UseEmptyString.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.UseEmptyString));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNodeVariableDeclaration, SyntaxKind.SimpleMemberAccessExpression);

        private static void AnalyzeNodeVariableDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var expression = context.Node as MemberAccessExpressionSyntax;
            if (expression.ToString().ToLower() != "string.empty" || expression.Ancestors().OfType<AttributeArgumentSyntax>().Any())
                return;
            var diagnostic = Diagnostic.Create(Rule, expression.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}