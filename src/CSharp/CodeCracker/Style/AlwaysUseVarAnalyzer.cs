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
        internal static readonly DiagnosticDescriptor RuleNonPrimitives = new DiagnosticDescriptor(
            DiagnosticId.AlwaysUseVar.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.AlwaysUseVar));

        internal static readonly DiagnosticDescriptor RulePrimitives = new DiagnosticDescriptor(
            DiagnosticId.AlwaysUseVarOnPrimitives.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.AlwaysUseVarOnPrimitives));

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

            if (variableDeclaration.Type.IsVar) return;
            var isDynamic = (variableDeclaration.Type as IdentifierNameSyntax)?.Identifier.ValueText == "dynamic";

            var semanticModel = context.SemanticModel;
            var variableTypeName = localDeclaration.Declaration.Type;
            var variableType = semanticModel.GetTypeInfo(variableTypeName).ConvertedType;

            foreach (var variable in variableDeclaration.Variables)
            {
                if (variable.Initializer == null) return;
                var conversion = semanticModel.ClassifyConversion(variable.Initializer.Value, variableType);
                if (!conversion.IsIdentity) return;
                if (!isDynamic) continue;
                var expressionReturnType = semanticModel.GetTypeInfo(variable.Initializer.Value);
                if (expressionReturnType.Type.SpecialType == SpecialType.System_Object) return;
            }

            var rule = IsPrimitvie(variableType) ? RulePrimitives : RuleNonPrimitives;
            var diagnostic = Diagnostic.Create(rule, variableDeclaration.Type.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsPrimitvie(ITypeSymbol typeSymbol)
        {
            switch (typeSymbol.SpecialType)
            {
                case SpecialType.System_Boolean:
                case SpecialType.System_Byte:
                case SpecialType.System_SByte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_IntPtr:
                case SpecialType.System_UIntPtr:
                case SpecialType.System_Char:
                case SpecialType.System_Double:
                case SpecialType.System_Single:
                    return true;
                default:
                    return false;
            }
        }
    }
}