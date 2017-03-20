using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System.Collections.Generic;
using System;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReadOnlyComplexTypesAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Complex fields must be readonly";
        internal const string Message = "Make '{0}' readonly";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "Complex fields must be readonly";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ReadOnlyComplexTypes.ToDiagnosticId(),
            Title,
            Message,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ReadOnlyComplexTypes));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode,
                                                                      new[] { SyntaxKind.FieldDeclaration });

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var fieldDeclaration = context.Node as FieldDeclarationSyntax;
            var variable = fieldDeclaration?.Declaration.Variables.LastOrDefault();
            if (variable?.Initializer == null) return;
            var semanticModel = context.SemanticModel;
            var fieldSymbol = semanticModel.GetDeclaredSymbol(variable) as IFieldSymbol;
            if (!IsComplexValueType(semanticModel, fieldDeclaration)) return;
            if (!CanBeMadeReadonly(fieldSymbol)) return;
            ReportDiagnostic(context, variable, variable.Initializer.Value);
        }
        private static bool IsComplexValueType(SemanticModel semanticModel, FieldDeclarationSyntax fieldDeclaration)
        {
            var fieldTypeName = fieldDeclaration.Declaration.Type;
            var fieldType = semanticModel.GetTypeInfo(fieldTypeName).ConvertedType;
            return fieldType.IsValueType && !(fieldType.TypeKind == TypeKind.Enum || fieldType.IsPrimitive());
        }

        private static bool CanBeMadeReadonly(IFieldSymbol fieldSymbol)
        {
            return (fieldSymbol.DeclaredAccessibility == Accessibility.NotApplicable
                || fieldSymbol.DeclaredAccessibility == Accessibility.Private)
                && !fieldSymbol.IsReadOnly
                && !fieldSymbol.IsConst;
        }

        private static void ReportDiagnostic(SyntaxNodeAnalysisContext context, VariableDeclaratorSyntax variable, ExpressionSyntax initializerValue)
        {
            var props = new Dictionary<string, string> { { "identifier", variable.Identifier.Text } }.ToImmutableDictionary();
            var diag = Diagnostic.Create(Rule, variable.GetLocation(), props, initializerValue.ToString());
            context.ReportDiagnostic(diag);
        }
    }
}
