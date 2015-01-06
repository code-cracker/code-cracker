using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InterfaceNameAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0062";
        internal const string Title = "You should add letter 'I' before interface name.";
        internal const string MessageFormat = "Consider naming interfaces starting with 'I'.";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Consider naming interfaces starting with 'I'.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeClass, SyntaxKind.InterfaceDeclaration);
        }

        private static void AnalyzeClass(SyntaxNodeAnalysisContext context)
        {
            var invocationExpression = (InterfaceDeclarationSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            var name = invocationExpression.Identifier.ToString().ToUpper();
            if (name.StartsWith("I")) return;
            var error = string.Format(MessageFormat, MessageFormat);
            var diag = Diagnostic.Create(Rule, invocationExpression.GetLocation(), error);
            context.ReportDiagnostic(diag);
        }
    }
}