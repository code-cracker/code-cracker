using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.Style
{
    public class ObjectInitializerAnalyzer : CSharpAnalyzer
    {
        private const string description = "When possible an object initializer should be used to " +
            "initialize the properties of an object instead of multiple assignments.";
        public ObjectInitializerAnalyzer() : base(new DiagnosticDescriptorInfo
        {
            Id = DiagnosticId.ObjectInitializer_LocalDeclaration,
            Title = "Use object initializer",
            Message = "You can use initializers in here.",
            Category = SupportedCategories.Style,
            Description = description,
            Severity = DiagnosticSeverity.Warning
        }, new DiagnosticDescriptorInfo
        {
            Id = DiagnosticId.ObjectInitializer_Assignment,
            Title = "Use object initializer",
            Message = "You can use initializers in here.",
            Category = SupportedCategories.Style,
            Description = description,
            Severity = DiagnosticSeverity.Warning
        }) { }

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
            ReportDiagnostic(context, expressionStatement.GetLocation(), 1);
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
            ReportDiagnostic(context, localDeclarationStatement.GetLocation(), 0);
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