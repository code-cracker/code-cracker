using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using RoslynExts.CS;

namespace CodeCracker.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class NameOfAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0021";
        internal const string Title = "You should use nameof instead of the parameter string";
        internal const string MessageFormat = "Use 'nameof({0})' instead of specifying the parameter name.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "In C#6 the nameof() operator should be used to specify the name of a parameter instead of "
            + "a string literal as it produce code that is easier to refactor.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyzer, SyntaxKind.StringLiteralExpression);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var stringLiteral = context.Node as LiteralExpressionSyntax;
            if (string.IsNullOrWhiteSpace(stringLiteral?.Token.ValueText)) return;
            var methodDeclaration = stringLiteral.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().FirstOrDefault();

            if (methodDeclaration != null)
            {
                var methodParameters = methodDeclaration.ParameterList.Parameters;
                if (!AreEqual(stringLiteral, methodParameters)) return;
            }
            else
            {
                var constructorDeclaration = stringLiteral.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().FirstOrDefault();
                if (constructorDeclaration != null)
                {
                    var constructorParameters = constructorDeclaration.ParameterList.Parameters;
                    if (!AreEqual(stringLiteral, constructorParameters)) return;
                }
                else return;
            }
            var diagnostic = Diagnostic.Create(Rule, stringLiteral.GetLocation(), stringLiteral.Token.Value);
            context.ReportDiagnostic(diagnostic);
        }

        private bool AreEqual(LiteralExpressionSyntax stringLiteral, SeparatedSyntaxList<ParameterSyntax> parameters)
        {
            return parameters.Any(m => m.Identifier.Value.ToString() == stringLiteral.Token.Value.ToString());
        }
    }
}