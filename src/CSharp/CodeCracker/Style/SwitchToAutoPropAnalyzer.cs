using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class SwitchToAutoPropAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.SwitchToAutoPropAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.SwitchToAutoPropAnalyzer_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.SwitchToAutoPropAnalyzer_Description), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Style;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.SwitchToAutoProp.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.SwitchToAutoProp));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeActionForVersionLower(LanguageVersion.CSharp6, AnalyzePropertyWithoutFieldInitializer, SyntaxKind.PropertyDeclaration);
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, AnalyzePropertyWithFieldInitializer, SyntaxKind.PropertyDeclaration);
        }

        private static void AnalyzePropertyWithFieldInitializer(SyntaxNodeAnalysisContext context) => AnalyzeProperty(context, true);
        private static void AnalyzePropertyWithoutFieldInitializer(SyntaxNodeAnalysisContext context) => AnalyzeProperty(context, false);

        private static void AnalyzeProperty(SyntaxNodeAnalysisContext context, bool canHaveFieldInitializer)
        {
            if (context.IsGenerated()) return;
            var property = (PropertyDeclarationSyntax)context.Node;
            if (property.AccessorList?.Accessors.Count != 2) return;
            if (property.AccessorList.Accessors.Any(a => a.Body == null)) return;
            if (property.AccessorList.Accessors.Any(a => a.Body.Statements.Count != 1)) return;
            if (property.AccessorList.Accessors.Any(a => a.Body.Statements.Count != 1)) return;
            var getter = property.AccessorList.Accessors.First(a => a.Keyword.ValueText == "get");
            var getterReturn = getter.Body.Statements.First() as ReturnStatementSyntax;
            if (getterReturn == null) return;
            var setter = property.AccessorList.Accessors.First(a => a.Keyword.ValueText == "set");
            var setterExpressionStatement = setter.Body.Statements.First() as ExpressionStatementSyntax;
            var setterAssignmentExpression = setterExpressionStatement?.Expression as AssignmentExpressionSyntax;
            if (setterAssignmentExpression == null) return;
            var returnIdentifier = getterReturn.Expression as IdentifierNameSyntax;
            if (returnIdentifier == null) return;
            var semanticModel = context.SemanticModel;
            var returnIdentifierSymbol = semanticModel.GetSymbolInfo(returnIdentifier).Symbol;
            if (returnIdentifierSymbol == null) return;
            var assignmentLeftIdentifier = setterAssignmentExpression.Left as IdentifierNameSyntax;
            if (assignmentLeftIdentifier == null) return;
            var assignmentLeftIdentifierSymbol = semanticModel.GetSymbolInfo(assignmentLeftIdentifier).Symbol;
            if (!assignmentLeftIdentifierSymbol.Equals(returnIdentifierSymbol)) return;
            var assignmentRightIdentifier = setterAssignmentExpression.Right as IdentifierNameSyntax;
            if (assignmentRightIdentifier == null) return;
            if (assignmentRightIdentifier.Identifier.Text != "value") return;
            if (assignmentLeftIdentifierSymbol.Kind != SymbolKind.Field) return;
            var backingFieldClassSymbol = assignmentLeftIdentifierSymbol.ContainingType;
            var propertySymbol = semanticModel.GetDeclaredSymbol(property);
            var propertyClassSymbol = propertySymbol.ContainingType;
            if (!propertyClassSymbol.Equals(backingFieldClassSymbol)) return;
            if (!canHaveFieldInitializer)
            {
                var variableDeclarator = assignmentLeftIdentifierSymbol.DeclaringSyntaxReferences.First().GetSyntax() as VariableDeclaratorSyntax;
                if (variableDeclarator.Initializer != null) return;
            }
            var diag = Diagnostic.Create(Rule, property.GetLocation(), property.Identifier.Text);
            context.ReportDiagnostic(diag);
        }
    }
}