using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StringBuilderInLoopAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Don't concatenate strings in loops";
        internal const string MessageFormat = "Don't concatenate '{0}' in a loop";
        internal const string Category = SupportedCategories.Performance;
        const string Description = "Do not concatenate a string on a loop. It will alocate a lot of memory."
            + "Use a StringBuilder instead. It will will require less allocation, less garbage collector work, less CPU cycles, and less overall time.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.StringBuilderInLoop.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.StringBuilderInLoop));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeAssignment, SyntaxKind.AddAssignmentExpression, SyntaxKind.SimpleAssignmentExpression);

        private static void AnalyzeAssignment(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var assignmentExpression = (AssignmentExpressionSyntax)context.Node;
            var loopStatement = assignmentExpression.FirstAncestorOfType(typeof(WhileStatementSyntax),
                typeof(ForStatementSyntax),
                typeof(ForEachStatementSyntax),
                typeof(DoStatementSyntax));
            if (loopStatement == null) return;
            var semanticModel = context.SemanticModel;
            var arrayAccess = assignmentExpression.Left as ElementAccessExpressionSyntax;
            var symbolForAssignment = arrayAccess != null
                ? semanticModel.GetSymbolInfo(arrayAccess.Expression).Symbol
                : semanticModel.GetSymbolInfo(assignmentExpression.Left).Symbol;
            ITypeSymbol type;
            if (symbolForAssignment is IPropertySymbol) type = ((IPropertySymbol)symbolForAssignment).Type;
            else if (symbolForAssignment is ILocalSymbol) type = ((ILocalSymbol)symbolForAssignment).Type;
            else if (symbolForAssignment is IFieldSymbol) type = ((IFieldSymbol)symbolForAssignment).Type;
            else return;
            if (type == null) return;
            if (type.TypeKind == TypeKind.Array)
            {
                if ((type as IArrayTypeSymbol)?.ElementType?.SpecialType != SpecialType.System_String) return;
            }
            else if (type.Name != "String") return;
            // Do not analyze a string declared within the loop.
            if (symbolForAssignment is ILocalSymbol && loopStatement.DescendantTokens(((ILocalSymbol)symbolForAssignment).DeclaringSyntaxReferences[0].Span).Any()) return;
            var memberAccess = assignmentExpression.Left as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                var memberAccessExpressionSymbol = semanticModel.GetSymbolInfo(memberAccess.Expression).Symbol as ILocalSymbol;
                if (memberAccessExpressionSymbol != null)
                {
                    if (loopStatement.DescendantTokens(memberAccessExpressionSymbol.DeclaringSyntaxReferences[0].Span).Any())
                        return;
                }
            }
            if (assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression))
            {
                if (!(assignmentExpression.Right?.IsKind(SyntaxKind.AddExpression) ?? false)) return;
                var identifierOnConcatExpression = ((BinaryExpressionSyntax)assignmentExpression.Right).Left as IdentifierNameSyntax;
                if (identifierOnConcatExpression == null) return;
                var symbolOnIdentifierOnConcatExpression = semanticModel.GetSymbolInfo(identifierOnConcatExpression).Symbol;
                if (!symbolForAssignment.Equals(symbolOnIdentifierOnConcatExpression)) return;
            }
            else if (!assignmentExpression.IsKind(SyntaxKind.AddAssignmentExpression)) return;
            var assignmentExpressionLeft = assignmentExpression.Left.ToString();
            var properties = new Dictionary<string, string> { { nameof(assignmentExpressionLeft), assignmentExpressionLeft } }.ToImmutableDictionary();
            var diagnostic = Diagnostic.Create(Rule, assignmentExpression.GetLocation(), properties, assignmentExpressionLeft);
            context.ReportDiagnostic(diagnostic);
        }
    }
}