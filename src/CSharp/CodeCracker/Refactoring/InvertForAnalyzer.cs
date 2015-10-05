using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class InvertForAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Invert the for loop counting.";
        internal const string MessageFormat = "Make it a for loop that {0} the counter.";
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.InvertFor.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.InvertFor));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ForStatement);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var @for = (ForStatementSyntax)context.Node;
            if (@for.Declaration == null && @for.Initializers.Count == 0) return;
            if (@for.Declaration != null && @for.Declaration.Variables.Count != 1) return;
            if (!HasAcceptableIncrementors(@for)) return;
            if (!HasConditionCompatibleWithTheIncrementor(@for)) return;
            if (!AreDeclarationConditionAndIncrementorUsingTheSameVariable(@for)) return;

            var diagnostic = Diagnostic.Create(Rule, @for.GetLocation(), IsPostIncrement(@for.Incrementors[0]) ? "decrement" : "increment");
            context.ReportDiagnostic(diagnostic);
        }

        internal static bool IsPostIncrement(ExpressionSyntax expression)
        {
            var unary = expression as PostfixUnaryExpressionSyntax;
            if (unary == null) return false;
            return unary.IsKind(SyntaxKind.PostIncrementExpression);
        }

        static bool HasAcceptableIncrementors(ForStatementSyntax @for)
        {
            if (@for.Incrementors.Count != 1) return false;
            var unary = @for.Incrementors[0] as PostfixUnaryExpressionSyntax;
            if (unary == null) return false;
            return unary.IsKind(SyntaxKind.PostIncrementExpression) || unary.IsKind(SyntaxKind.PostDecrementExpression);
        }

        static bool HasConditionCompatibleWithTheIncrementor(ForStatementSyntax @for)
        {
            var condition = @for.Condition as BinaryExpressionSyntax;
            var postIncrement = IsPostIncrement(@for.Incrementors[0]);
            return condition != null &&
                (
                    (postIncrement && condition.OperatorToken.IsKind(SyntaxKind.LessThanToken)) ||
                    (!postIncrement && condition.OperatorToken.IsKind(SyntaxKind.GreaterThanEqualsToken))
                );
        }

        static bool AreDeclarationConditionAndIncrementorUsingTheSameVariable(ForStatementSyntax @for)
        {
            SyntaxToken reference;

            if (@for.Declaration != null)
            {
                reference = @for.Declaration.Variables[0].Identifier;
            }
            else
            {
                var initializer = @for.Initializers[0] as AssignmentExpressionSyntax;
                if (initializer == null) return false;
                var name = initializer.Left as IdentifierNameSyntax;
                if (name == null) return false;
                reference = name.Identifier;
            }

            var condition = @for.Condition as BinaryExpressionSyntax;
            var incrementor = @for.Incrementors[0] as PostfixUnaryExpressionSyntax;

            if (!(condition.Left is IdentifierNameSyntax)) return false;
            if (!(incrementor.Operand is IdentifierNameSyntax)) return false;

            var conditionVariableIdentifier = (condition.Left as IdentifierNameSyntax).Identifier;
            var incrementorVariableIdentifier = (incrementor.Operand as IdentifierNameSyntax).Identifier;

            return reference.Text == conditionVariableIdentifier.Text
                && reference.Text == incrementorVariableIdentifier.Text;
        }
    }
}