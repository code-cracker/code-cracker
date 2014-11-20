using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ObjectInitializerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticIdLocalDeclaration = "CC0008";
        internal const string TitleLocalDeclaration = "Use object initializer";
        internal const string MessageFormat = "{0}";
        internal const string Category = "Syntax";
        public const string DiagnosticIdAssignment = "CC0009";
        internal const string TitleAssignment = "Use object initializer";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticIdLocalDeclaration, TitleLocalDeclaration, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

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

            var diagnostic = Diagnostic.Create(Rule, expressionStatement.GetLocation(), "You can use initializers in here.");
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

            var diagnostic = Diagnostic.Create(Rule, localDeclarationStatement.GetLocation(), "You can use initializers in here.");
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