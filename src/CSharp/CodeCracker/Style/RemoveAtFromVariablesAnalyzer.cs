using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveAtFromVariablesAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Remove @ from variables that are not keywords.";
        internal const string MessageFormat = "Remove @ from variables that are not keywords.";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Usage of @ on variable names only when it is a CSharp keyword";

        internal static readonly DiagnosticDescriptor RuleNonPrimitives = new DiagnosticDescriptor(
            DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords));

        internal static readonly DiagnosticDescriptor RulePrimitives = new DiagnosticDescriptor(
            DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(RuleNonPrimitives, RulePrimitives);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;
            if (localDeclaration.IsConst) return;

            var variableDeclaration = localDeclaration.ChildNodes()
                .OfType<VariableDeclarationSyntax>()
                .FirstOrDefault();

            var semanticModel = context.SemanticModel;
            var variableTypeName = localDeclaration.Declaration.Type;
            var variableType = semanticModel.GetTypeInfo(variableTypeName).ConvertedType;

            foreach (var variable in variableDeclaration.Variables)
            {
                if (variable.Identifier.Text.StartsWith(@"@"))
                {
                    var identifier = variable.Identifier.ValueText;
                    if (identifier.IsCSharpKeyword()) return;

                    var rule = variableType.IsPrimitive() ? RulePrimitives : RuleNonPrimitives;
                    var diagnostic = Diagnostic.Create(rule, variableDeclaration.Type.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}