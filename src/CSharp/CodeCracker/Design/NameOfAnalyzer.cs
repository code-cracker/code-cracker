using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NameOfAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "You should use nameof instead of the parameter string";
        internal const string MessageFormat = "Use 'nameof({0})' instead of specifying the parameter name.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "In C#6 the nameof() operator should be used to specify the name of a parameter instead of "
            + "a string literal as it produce code that is easier to refactor.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.NameOf.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.NameOf));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyzer, SyntaxKind.StringLiteralExpression);

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var stringLiteral = context.Node as LiteralExpressionSyntax;
            if (string.IsNullOrWhiteSpace(stringLiteral?.Token.ValueText)) return;
            var parameters = GetParameters(stringLiteral);
            if (!parameters.Any()) return;
            var attribute = stringLiteral.FirstAncestorOfType<AttributeSyntax>();
            var method = stringLiteral.FirstAncestorOfType(typeof(MethodDeclarationSyntax), typeof(ConstructorDeclarationSyntax)) as BaseMethodDeclarationSyntax;
            if (attribute != null && method.AttributeLists.Any(a => a.Attributes.Contains(attribute))) return;
            if (!AreEqual(stringLiteral, parameters)) return;
            var diagnostic = Diagnostic.Create(Rule, stringLiteral.GetLocation(), stringLiteral.Token.Value);
            context.ReportDiagnostic(diagnostic);
        }

        public SeparatedSyntaxList<ParameterSyntax> GetParameters(SyntaxNode node)
        {
            var methodDeclaration = node.FirstAncestorOfType<MethodDeclarationSyntax>();
            SeparatedSyntaxList<ParameterSyntax> parameters;
            if (methodDeclaration != null)
            {
                parameters = methodDeclaration.ParameterList.Parameters;
            }
            else
            {
                var constructorDeclaration = node.FirstAncestorOfType<ConstructorDeclarationSyntax>();
                if (constructorDeclaration != null)
                    parameters = constructorDeclaration.ParameterList.Parameters;
                else return new SeparatedSyntaxList<ParameterSyntax>();
            }
            return parameters;
        }

        private bool AreEqual(LiteralExpressionSyntax stringLiteral, SeparatedSyntaxList<ParameterSyntax> parameters) =>
            parameters.Any(m => m.Identifier.Value.ToString() == stringLiteral.Token.Value.ToString());
    }
}