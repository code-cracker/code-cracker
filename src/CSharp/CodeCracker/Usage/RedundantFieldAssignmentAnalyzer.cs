using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RedundantFieldAssignmentAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Redundant field assignment";
        internal const string MessageFormat = "Field {0} is assigning to default value {1}. Remove the assignment.";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "It's recommend not to assign the default value to a field as a performance optimization.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RedundantFieldAssignment.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            customTags: WellKnownDiagnosticTags.Unnecessary,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RedundantFieldAssignment));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeFieldDeclaration, SyntaxKind.FieldDeclaration);

        private static void AnalyzeFieldDeclaration(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var fieldDeclaration = context.Node as FieldDeclarationSyntax;
            var variable = fieldDeclaration?.Declaration.Variables.LastOrDefault();
            if (variable?.Initializer == null) return;
            if (fieldDeclaration.Modifiers.Any(SyntaxKind.ConstKeyword)) return;
            var initializerValue = variable.Initializer.Value;
            if (initializerValue.IsKind(SyntaxKind.DefaultExpression))
            {
                ReportDiagnostic(context, variable, initializerValue);
                return;
            }
            var semanticModel = context.SemanticModel;
            var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
            if (fieldSymbol == null) return;
            if (!IsAssigningToDefault(fieldSymbol.Type, initializerValue, semanticModel)) return;
            ReportDiagnostic(context, variable, initializerValue);
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, VariableDeclaratorSyntax variable, ExpressionSyntax initializerValue)
        {
            var diag = Diagnostic.Create(Rule, variable.GetLocation(), variable.Identifier.ValueText, initializerValue.ToString());
            context.ReportDiagnostic(diag);
        }

        private static bool IsAssigningToDefault(ITypeSymbol fieldType, ExpressionSyntax initializerValue, SemanticModel semanticModel)
        {
            if (fieldType.IsReferenceType)
            {
                if (!initializerValue.IsKind(SyntaxKind.NullLiteralExpression)) return false;
            }
            else
            {
                if (!IsValueTypeAssigningToDefault(fieldType, initializerValue, semanticModel)) return false;
            }
            return true;
        }

        private static bool IsValueTypeAssigningToDefault(ITypeSymbol fieldType, ExpressionSyntax initializerValue, SemanticModel semanticModel)
        {
            switch (fieldType.SpecialType)
            {
                case SpecialType.System_Boolean:
                    {
                        var literal = initializerValue as LiteralExpressionSyntax;
                        if (literal == null) return false;
                        var boolValue = (bool)literal.Token.Value;
                        if (boolValue) return false;
                        break;
                    }
                case SpecialType.System_SByte:
                case SpecialType.System_Byte:
                case SpecialType.System_Int16:
                case SpecialType.System_UInt16:
                case SpecialType.System_Int32:
                case SpecialType.System_UInt32:
                case SpecialType.System_Int64:
                case SpecialType.System_UInt64:
                case SpecialType.System_Decimal:
                case SpecialType.System_Single:
                case SpecialType.System_Double:
                    if (initializerValue.ToString() != "0")
                    {
                        var literal = initializerValue as LiteralExpressionSyntax;
                        if (literal == null) return false;
                        var possibleZero = Convert.ToDouble(literal.Token.Value);
                        if (possibleZero != 0) return false;
                    }
                    break;
                case SpecialType.System_IntPtr:
                    {
                        var memberAccess = initializerValue as MemberAccessExpressionSyntax;
                        if (memberAccess == null) return false;
                        var memberAccessFieldSymbol = semanticModel.GetSymbolInfo(memberAccess).Symbol as IFieldSymbol;
                        if (memberAccessFieldSymbol?.ToString() != "System.IntPtr.Zero") return false;
                        break;
                    }
                case SpecialType.System_UIntPtr:
                    {
                        var memberAccess = initializerValue as MemberAccessExpressionSyntax;
                        if (memberAccess == null) return false;
                        var memberAccessFieldSymbol = semanticModel.GetSymbolInfo(memberAccess).Symbol as IFieldSymbol;
                        if (memberAccessFieldSymbol?.ToString() != "System.UIntPtr.Zero") return false;
                        break;
                    }
                case SpecialType.System_DateTime:
                    {
                        var memberAccess = initializerValue as MemberAccessExpressionSyntax;
                        if (memberAccess == null) return false;
                        var memberAccessFieldSymbol = semanticModel.GetSymbolInfo(memberAccess).Symbol as IFieldSymbol;
                        if (memberAccessFieldSymbol?.ToString() != "System.DateTime.MinValue") return false;
                        break;
                    }
                //case SpecialType.System_Enum: //does not work, enums come back as none. Bug on roslyn? See solution below.
                default:
                    if (fieldType.TypeKind != TypeKind.Enum) return false;
                    if (initializerValue.ToString() != "0")
                    {
                        var literal = initializerValue as LiteralExpressionSyntax;
                        if (literal == null) return false;
                        var possibleZero = Convert.ToDouble(literal.Token.Value);
                        if (possibleZero != 0) return false;
                    }
                    break;
            }
            return true;
        }
    }
}