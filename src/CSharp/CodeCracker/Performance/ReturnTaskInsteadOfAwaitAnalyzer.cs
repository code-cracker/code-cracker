﻿using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using CodeCracker.Properties;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReturnTaskInsteadOfAwaitAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ReturnTaskInsteadOfAwait_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ReturnTaskInsteadOfAwait_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ReturnTaskInsteadOfAwait_Description), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Performance;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ReturnTaskInsteadOfAwait.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ReturnTaskInsteadOfAwait));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var methodDecl = (context.Node as MethodDeclarationSyntax);
            if (!methodDecl.Modifiers.Any(SyntaxKind.AsyncKeyword)) return;
            if (methodDecl.Body == null || methodDecl.ExpressionBody != null) return;

            var awaits = new List<StatementSyntax>();
            var anyReturns = false;
            foreach (var child in methodDecl.Body.DescendantNodes())
            {
                if (child.IsKind(SyntaxKind.AwaitExpression))
                {
                    if (child.Ancestors().Any(ancestor => ancestor.IsLoopStatement())) return;
                    var awaitStatement = child.FirstAncestorOrSelfThatIsAStatement();
                    awaits.Add(awaitStatement);
                }
                else if (child.IsKind(SyntaxKind.ReturnStatement))
                    anyReturns = true;
            }
            if (awaits.Count == 0) return;

            var returnType = context.SemanticModel.GetTypeInfo(methodDecl.ReturnType);

            if (returnType.Type.SpecialType == SpecialType.System_Void ||
                returnType.Type.MetadataName.Equals(nameof(Task)))
            {
                if (anyReturns) return;
                var checkedBranches = new Dictionary<StatementSyntax, bool>();
                foreach (var awaitStatement in awaits)
                {
                    var lastSiblingOfAwaitParent = SiblingsAndSelf(awaitStatement)
                        .Last(node => !IsExpressionLessStatement(node));
                    if (lastSiblingOfAwaitParent != awaitStatement) return;
                    if (!CheckBranching(awaitStatement, checkedBranches)) return;
                }
                if (checkedBranches.ContainsValue(false)) return;
            }
            else
            {
                foreach (var awaitStatements in awaits)
                {
                    if (!awaitStatements.IsKind(SyntaxKind.ReturnStatement)) return;
                }
            }

            var name = methodDecl.Identifier.ToString();
            var diagnostic = Diagnostic.Create(Rule, methodDecl.GetLocation(), name);
            context.ReportDiagnostic(diagnostic);
        }

        /// <summary>
        /// Checks if the branch contains an await in all as the last statement. Returns true if the checking should continue.
        /// </summary>
        /// <param name="statement">Statement to look at</param>
        /// <param name="checkedBranches">Lookup for the branches already checked.</param>
        /// <returns>True if the checking should continue.</returns>
        private static bool CheckBranching(StatementSyntax statement, Dictionary<StatementSyntax, bool> checkedBranches)
        {
            if (statement == null) return true;
            if (checkedBranches.ContainsKey(statement) && checkedBranches[statement]) return true;
            checkedBranches[statement] = false;

            if (statement.IsKind(SyntaxKind.IfStatement))
            {
                var IfStatement = statement as IfStatementSyntax;
                var ifHasAwait = BranchContainsAwait(IfStatement.Statement, true, checkedBranches);
                var elseHasAwait = BranchContainsAwait(IfStatement.Else?.Statement, false, checkedBranches);
                if (!ifHasAwait || !elseHasAwait) return true;

                var lastSibling = SiblingsAndSelf(IfStatement).Last(statmement => !IsExpressionLessStatement(statmement));
                if (lastSibling != IfStatement) return true;
            }
            else if (statement.IsKind(SyntaxKind.SwitchStatement))
            {
                var SwitchStatement = statement as SwitchStatementSyntax;
                var containsDefault = false;
                foreach (var section in SwitchStatement.Sections)
                {
                    if (section.Labels.Any(SyntaxKind.DefaultSwitchLabel)) containsDefault = true;
                    var lastStatementInSection = section.Statements.Last(statmement => !IsExpressionLessStatement(statmement));
                    var hasAwait = BranchContainsAwait(lastStatementInSection, false, checkedBranches);
                    if (!hasAwait) return true;
                }
                if (!containsDefault) return false;
            }
            checkedBranches[statement] = true;
            return CheckBranching(statement.Parent.FirstAncestorOrSelfThatIsAStatement(), checkedBranches);
        }

        private static bool BranchContainsAwait(StatementSyntax statement, bool nestedStatments, Dictionary<StatementSyntax, bool> checkedBranches)
        {
            if (statement == null) return false;
            if (checkedBranches.ContainsKey(statement) && checkedBranches[statement]) return true;

            var singleStatement = statement.GetSingleStatementFromPossibleBlock();
            if (singleStatement != null && singleStatement != statement)
            {
                if (checkedBranches.ContainsKey(singleStatement) && checkedBranches[singleStatement]) return true;
                return GetAwaitExpression(singleStatement) != null;
            }

            if (nestedStatments)
            {
                var statements = statement.ChildNodes();
                var lastStatement = statements.Last(statmement => !IsExpressionLessStatement(statmement));
                return GetAwaitExpression(lastStatement) != null;
            }
            else
                return GetAwaitExpression(statement) != null;
        }

        private static AwaitExpressionSyntax GetAwaitExpression(SyntaxNode syntaxNode)
        {
            if (syntaxNode.IsKind(SyntaxKind.ExpressionStatement))
            {
                var statementExpr = (syntaxNode as ExpressionStatementSyntax);
                if (statementExpr.Expression.IsKind(SyntaxKind.AwaitExpression))
                {
                    return statementExpr.Expression as AwaitExpressionSyntax;
                }
            }
            else if (syntaxNode.IsKind(SyntaxKind.AwaitExpression))
            {
                return syntaxNode as AwaitExpressionSyntax;
            }
            return null;
        }

        private static IEnumerable<SyntaxNode> SiblingsAndSelf(SyntaxNode retStatment) => retStatment.Parent.ChildNodes();

        private static bool IsExpressionLessStatement(SyntaxNode node) => node.IsKind(SyntaxKind.ReturnStatement, SyntaxKind.ElseClause, SyntaxKind.BreakStatement);
    }
}