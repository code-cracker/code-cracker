using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class MakeLocalVariableConstWhenItIsPossibleAnalyzer :
        DiagnosticAnalyzer
    {
        internal const string Title = "Make Local Variable Constant.";
        internal const string MessageFormat = "This variable can be made const.";
        internal const string Category = SupportedCategories.Performance;
        const string Description = "This variable is assigned a constant value and never changed it can be made 'const'";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.MakeLocalVariableConstWhenItIsPossible.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.MakeLocalVariableConstWhenItIsPossible));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.LocalDeclarationStatement);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var localDeclaration = (LocalDeclarationStatementSyntax)context.Node;
            var semanticModel = context.SemanticModel;

            if (!localDeclaration.IsConst
                && IsDeclarationConstFriendly(localDeclaration, semanticModel)
                && AreVariablesOnlyWrittenInsideDeclaration(localDeclaration, semanticModel) )
            {
                var diagnostic = Diagnostic.Create(Rule, localDeclaration.GetLocation());
                context.ReportDiagnostic(diagnostic);
            }
        }

        static bool IsDeclarationConstFriendly(LocalDeclarationStatementSyntax declaration, SemanticModel semanticModel)
        {
            // all variables could be const?
            foreach (var variable in declaration.Declaration.Variables)
            {
                if (variable.Initializer == null) return false;
                if (variable.Initializer.Value is InterpolatedStringExpressionSyntax) return false;

                // is constant
                var constantValue = semanticModel.GetConstantValue(variable.Initializer.Value);
                var valueIsConstant = constantValue.HasValue;
                if (!valueIsConstant) return false;

                // if reference type, value is null?
                var variableTypeName = declaration.Declaration.Type;
                var variableType = semanticModel.GetTypeInfo(variableTypeName).ConvertedType;
                if (variableType.TypeKind == TypeKind.Pointer) return false;
                if (variableType.IsReferenceType && variableType.SpecialType != SpecialType.System_String && constantValue.Value != null) return false;

                // nullable?
                if (variableType.OriginalDefinition?.SpecialType == SpecialType.System_Nullable_T) return false;

                // value can be converted to variable type?
                var conversion = semanticModel.ClassifyConversion(variable.Initializer.Value, variableType);
                if (!conversion.Exists || conversion.IsUserDefined) return false;
            }
            return true;
        }

        static bool AreVariablesOnlyWrittenInsideDeclaration(LocalDeclarationStatementSyntax declaration, SemanticModel semanticModel)
        {
            var dfa = semanticModel.AnalyzeDataFlow(declaration);
            var symbols = from variable in declaration.Declaration.Variables
                          select semanticModel.GetDeclaredSymbol(variable);
            var result = !symbols.Any(s => dfa.WrittenOutside.Contains(s));
            return result;
        }
    }
}