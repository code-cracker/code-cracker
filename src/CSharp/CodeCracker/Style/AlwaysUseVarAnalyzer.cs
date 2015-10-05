using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AlwaysUseVarAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "You should use 'var' whenever possible.";
        internal const string MessageFormat = "Use 'var' instead of specifying the type name.";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Usage of an implicit type improve readability of the code.\r\n"
            + "Code depending on types for their readability should be refactored with better variable "
            + "names or by introducing well-named methods.";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.AlwaysUseVar.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description:Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.AlwaysUseVar));

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

            if (variableDeclaration.Type.IsVar) return;

            var semanticModel = context.SemanticModel;
            var variableTypeName = localDeclaration.Declaration.Type;
            var variableType = semanticModel.GetTypeInfo(variableTypeName).ConvertedType;

            foreach (var variable in variableDeclaration.Variables)
            {
                if (variable.Initializer == null) return;
                var conversion = semanticModel.ClassifyConversion(variable.Initializer.Value, variableType);
                if (!conversion.IsIdentity) return;
            }

            var diagnostic = Diagnostic.Create(Rule, variableDeclaration.Type.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}