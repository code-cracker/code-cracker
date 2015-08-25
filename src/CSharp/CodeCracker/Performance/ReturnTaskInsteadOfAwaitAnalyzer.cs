using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReturnTaskInsteadOfAwaitAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Remove async and return task directly.";
        internal const string MessageFormat = "This method can directly return a task.";
        internal const string Category = SupportedCategories.Performance;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ReturnTaskInsteadOfAwait.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ReturnTaskInsteadOfAwait));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var methodDecl = (context.Node as MethodDeclarationSyntax);
            if (!methodDecl.Modifiers.Any(SyntaxKind.AsyncKeyword)) return;
            if (context.IsGenerated()) return;

            var awaits = from child in methodDecl.Body.DescendantNodes(_ => true)
                         where child.IsKind(SyntaxKind.AwaitExpression)
                         let AwaitExpression = child as AwaitExpressionSyntax
                         select AwaitExpression;

            var blocks = new HashSet<BlockSyntax>();
            foreach (var await in awaits)
            {
                var block = await.FirstAncestorOfType<BlockSyntax>();
                if (blocks.Contains(block)) return;
                blocks.Add(block);
            }

            foreach (var block in blocks)
            {
                var lastStatement = block.ChildNodes().Last();
                if (!lastStatement.IsKind(SyntaxKind.ExpressionStatement)) return;
                var statement = lastStatement as ExpressionStatementSyntax;
                if (!statement.Expression.IsKind(SyntaxKind.AwaitExpression)) return;
            }

            var diagnostic = Diagnostic.Create(Rule, methodDecl.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}