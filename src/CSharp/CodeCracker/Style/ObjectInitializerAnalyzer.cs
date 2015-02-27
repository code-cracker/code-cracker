using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ObjectInitializerAnalyzer : DiagnosticAnalyzer
    {
        internal const string TitleLocalDeclaration = "Use object initializer";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Style;
        internal const string TitleAssignment = "Use object initializer";
        const string Description = "When possible an object initializer should be used to initialize the properties of an "
            + "object instead of multiple assignments.";

        internal static DiagnosticDescriptor RuleAssignment = new DiagnosticDescriptor(
            DiagnosticId.ObjectInitializer_Assignment.ToDiagnosticId(),
            TitleLocalDeclaration,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ObjectInitializer_Assignment));

        internal static DiagnosticDescriptor RuleLocalDeclaration = new DiagnosticDescriptor(
            DiagnosticId.ObjectInitializer_LocalDeclaration.ToDiagnosticId(),
            TitleLocalDeclaration,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ObjectInitializer_LocalDeclaration));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
            ImmutableArray.Create(RuleLocalDeclaration, RuleAssignment);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzerLocalDeclaration, SyntaxKind.LocalDeclarationStatement);
            context.RegisterSyntaxNodeAction(AnalyzerAssignment, SyntaxKind.ExpressionStatement);
        }

        private void AnalyzerAssignment(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var expressionStatement = context.Node as ExpressionStatementSyntax;
            if (!expressionStatement?.Expression?.IsKind(SyntaxKind.SimpleAssignmentExpression) ?? false) return;
            var assignmentExpression = (AssignmentExpressionSyntax)expressionStatement.Expression;
            var variableSymbol = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
            var assignmentExpressions = FindAssingmentExpressions(semanticModel, expressionStatement, variableSymbol);
            if (!assignmentExpressions.Any()) return;

            var diagnostic = Diagnostic.Create(RuleAssignment, expressionStatement.GetLocation(), "You can use initializers in here.");
            context.ReportDiagnostic(diagnostic);
        }

        private void AnalyzerLocalDeclaration(SyntaxNodeAnalysisContext context)
        {
            var semanticModel = context.SemanticModel;
            var localDeclarationStatement = context.Node as LocalDeclarationStatementSyntax;
            if (localDeclarationStatement == null) return;
            var objectCreationExpressions = localDeclarationStatement.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().ToList();
            if (objectCreationExpressions.Count != 1) return;
            if (localDeclarationStatement.Declaration.Variables.Count > 1) return;
            var variable = localDeclarationStatement.Declaration.Variables.Single();
            var variableSymbol = semanticModel.GetDeclaredSymbol(variable);
            var assignmentExpressions = FindAssingmentExpressions(semanticModel, localDeclarationStatement, variableSymbol);
            if (!assignmentExpressions.Any()) return;

            var diagnostic = Diagnostic.Create(RuleLocalDeclaration, localDeclarationStatement.GetLocation(), "You can use initializers in here.");
            context.ReportDiagnostic(diagnostic);
        }

        public static List<ExpressionStatementSyntax> FindAssingmentExpressions(SemanticModel semanticModel, StatementSyntax statement, ISymbol variableSymbol)
        {
            var blockParent = statement.FirstAncestorOrSelf<BlockSyntax>();
            var isBefore = true;
            var assignmentExpressions = new List<ExpressionStatementSyntax>();
            foreach (var blockStatement in blockParent.Statements)
            {
                if (isBefore)
                {
                    if (blockStatement.Equals(statement)) isBefore = false;
                }
                else
                {
                    var expressionStatement = blockStatement as ExpressionStatementSyntax;
                    if (expressionStatement == null) break;
                    var assignmentExpression = expressionStatement.Expression as AssignmentExpressionSyntax;
                    if (assignmentExpression == null || !assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression)) break;
                    var memberAccess = assignmentExpression.Left as MemberAccessExpressionSyntax;
                    if (memberAccess == null || !memberAccess.IsKind(SyntaxKind.SimpleMemberAccessExpression)) break;
                    var memberIdentifier = memberAccess.Expression as IdentifierNameSyntax;
                    if (memberIdentifier == null) break;
                    var propertyIdentifier = memberAccess.Name as IdentifierNameSyntax;
                    if (propertyIdentifier == null) break;
                    if (!semanticModel.GetSymbolInfo(memberIdentifier).Symbol.Equals(variableSymbol)) break;
                    assignmentExpressions.Add(expressionStatement);
                }
            }
            return assignmentExpressions;
        }
    }
}