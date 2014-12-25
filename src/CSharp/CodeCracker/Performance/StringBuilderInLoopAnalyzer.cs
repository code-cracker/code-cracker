using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringBuilderInLoopAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0039";
        internal const string Title = "Don't concatenate strings in loops";
        internal const string MessageFormat = "Don't concatenate '{0}' in a loop";
        internal const string Category = SupportedCategories.Performance;
        const string Description = "Do not concatenate a string on a loop. It will alocate a lot of memory."
            + "Use a StringBuilder instead. It will will require less allocation, less garbage collector work, less CPU cycles, and less overall time.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.AddAssignmentExpression, SyntaxKind.SimpleAssignmentExpression);
        }

        private void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            var assignmentExpression = (AssignmentExpressionSyntax)context.Node;
            var whileStatement = assignmentExpression.FirstAncestorOfType(typeof(WhileStatementSyntax),
                typeof(ForStatementSyntax),
                typeof(ForEachStatementSyntax),
                typeof(DoStatementSyntax));
            if (whileStatement == null) return;
            var semanticModel = context.SemanticModel;
            var symbolForAssignment = semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
            if (symbolForAssignment is IPropertySymbol && ((IPropertySymbol)symbolForAssignment).Type.Name != "String") return;
            if (symbolForAssignment is ILocalSymbol && ((ILocalSymbol)symbolForAssignment).Type.Name != "String") return;
            if (symbolForAssignment is IFieldSymbol && ((IFieldSymbol)symbolForAssignment).Type.Name != "String") return;

            if (assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                if (!(assignmentExpression.Right?.IsKind(SyntaxKind.AddExpression) ?? false)) return;
                var identifierOnConcatExpression = ((BinaryExpressionSyntax)assignmentExpression.Right).Left as IdentifierNameSyntax;
                if (identifierOnConcatExpression == null) return;
                var symbolOnIdentifierOnConcatExpression = semanticModel.GetSymbolInfo(identifierOnConcatExpression).Symbol;
                if (!symbolForAssignment.Equals(symbolOnIdentifierOnConcatExpression)) return;
            }
            else if (!assignmentExpression.IsKind(SyntaxKind.AddAssignmentExpression)) return;
            var diagnostic = Diagnostic.Create(Rule, assignmentExpression.GetLocation(), assignmentExpression.Left.ToString());
            context.ReportDiagnostic(diagnostic);
        }
    }
}