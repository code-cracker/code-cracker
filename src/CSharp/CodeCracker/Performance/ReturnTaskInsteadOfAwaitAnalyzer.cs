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
            if (methodDecl.ExpressionBody != null) return; // TODO: Workout for expressionBody


            var awaits = from child in methodDecl.Body.DescendantNodes(_ => true)
                         where child.IsKind(SyntaxKind.AwaitExpression)
                         let AwaitExpression = child as AwaitExpressionSyntax
                         select AwaitExpression;

            foreach (var await in awaits)
            {
                var ansestors = await.Ancestors();
                if (ansestors.Any(ansestor => IsLoopStatement(ansestor))) return;
            }

            var controlFlow = context.SemanticModel.AnalyzeControlFlow(methodDecl.Body);
            if (controlFlow.ReturnStatements.Length == 0)
            {
                //var returnType = context.SemanticModel.GetTypeInfo(methodDecl.ReturnType);
                //returnType.ConvertedType.spec;

                var lastStatement = methodDecl.Body.ChildNodes().Last();
                if (!lastStatement.IsKind(SyntaxKind.ExpressionStatement)) return;
                var statement = lastStatement as ExpressionStatementSyntax;
                if (!statement.Expression.IsKind(SyntaxKind.AwaitExpression)) return;


            }
            else
            {
                foreach (var await in awaits)
                {
                    if (!await.Parent.IsKind(SyntaxKind.ReturnStatement)) return;
                }
            }

            var diagnostic = Diagnostic.Create(Rule, methodDecl.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsLoopStatement(SyntaxNode note) => note.IsAnyKind(SyntaxKind.ForEachStatement, SyntaxKind.ForStatement, SyntaxKind.WhileStatement);


        async static Task FooAsync(bool x)
        {
            System.Math.Abs(12);
            if (x)
                await Task.Delay(200);
            else
                await Task.Delay(200);
        }
    }
}