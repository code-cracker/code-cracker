using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class IntroduceFieldFromConstructorAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Consider introduce field for constructor parameters.";
        internal const string MessageFormat = "Introduce a field for parameter: {0}";
        internal const string Category = SupportedCategories.Refactoring;
        const string Description = "Consider introduce field for constructor parameters.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.IntroduceFieldFromConstructor.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.IntroduceFieldFromConstructor));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeConstructor, SyntaxKind.ConstructorDeclaration);

        private static void AnalyzeConstructor(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var constructorMethod = (ConstructorDeclarationSyntax)context.Node;
            var parameters = constructorMethod.ParameterList.Parameters;

            if (constructorMethod.Body == null) return;

            var analysis = context.SemanticModel.AnalyzeDataFlow(constructorMethod.Body);
            if (!analysis.Succeeded) return;
            foreach (var par in parameters)
            {
                var parSymbol = context.SemanticModel.GetDeclaredSymbol(par);
                if(!analysis.ReadInside.Any(s => s.Equals(parSymbol)))
                {
                    var parameterName = par.Identifier.Text;
                    var properties = new Dictionary<string, string> { { nameof(parameterName), parameterName } }.ToImmutableDictionary();
                    var diag = Diagnostic.Create(Rule, par.GetLocation(), properties, parameterName);
                    context.ReportDiagnostic(diag);
                }
            }
        }
    }
}