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

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

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

                    var diagnostic = Diagnostic.Create(Rule, variableDeclaration.Type.GetLocation());
                    context.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}