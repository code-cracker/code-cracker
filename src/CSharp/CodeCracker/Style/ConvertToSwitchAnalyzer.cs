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
    public class ConvertToSwitchAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use 'switch'";
        internal const string MessageFormat = "You could use 'switch' instead of 'if'.";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Multiple 'if' and 'else if' on the same variable can be replaced with a 'switch'"
            + "on the variable\r\n\r\n"
            + "Note: This diagnostic trigger for 3 or more 'case' statements";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ConvertToSwitch.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ConvertToSwitch));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.IfStatement);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var ifStatement = (IfStatementSyntax)context.Node;

            // ignoring else if
            if (ifStatement.Parent is ElseClauseSyntax) return;

            // ignoring simple if statement
            if (ifStatement.Else == null) return;

            var nestedIfs = FindNestedIfs(ifStatement).ToArray();

            // ignoring less than 3 nested ifs
            if (nestedIfs.Length < 3) return;

            // ignoring when not all conditionals are "equals"
            IdentifierNameSyntax common = null;

            for (int i = 0; i < nestedIfs.Length; i++)
            {
                var condition = nestedIfs[i].Condition as BinaryExpressionSyntax;

                // all ifs should have binary expressions as conditions
                if (condition == null) return;

                // all conditions should be "equal"
                if (!condition.IsKind(SyntaxKind.EqualsExpression)) return;

                var left = condition.Left as IdentifierNameSyntax;
                // all conditions should have an identifier in the left
                if (left == null) return;

                if (i == 0)
                {
                    common = left;
                }
                else if (!left.Identifier.IsEquivalentTo(common.Identifier))
                {
                    // all conditions should have the same identifier in the left
                    return;
                }

                var right = context.SemanticModel.GetConstantValue(condition.Right);
                // only constants in the right side
                if (!right.HasValue) return;

            }

            var diagnostic = Diagnostic.Create(Rule, ifStatement.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        internal static IEnumerable<IfStatementSyntax> FindNestedIfs(IfStatementSyntax ifStatement)
        {
            do
            {
                yield return ifStatement;
                if (ifStatement.Else == null) yield break;
                ifStatement = ifStatement.Else.ChildNodes().FirstOrDefault() as IfStatementSyntax;
            } while (ifStatement != null);
        }
    }
}