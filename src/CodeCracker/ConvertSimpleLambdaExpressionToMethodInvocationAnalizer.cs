using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConvertSimpleLambdaExpressionToMethodInvocationAnalizer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0020";
        internal const string Title = "You should remove the lambda expression when it only invokes a method with the same signature";
        internal const string MessageFormat = "You should remove the lambda expression and pass just '{0}' instead.";
        internal const string Category = "Syntax";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Info, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.SimpleLambdaExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var lambda = (SimpleLambdaExpressionSyntax)context.Node;

            var methodInvoke = lambda.Body as InvocationExpressionSyntax;
            if (methodInvoke == null || methodInvoke.ArgumentList.Arguments.Count != 1) return;

            var methodArgument = methodInvoke.ArgumentList.Arguments[0].Expression as IdentifierNameSyntax;
            var lambdaParameter = lambda.Parameter;

            if (lambdaParameter.Identifier.Text != methodArgument.Identifier.Text) return;

            var methodName = (methodInvoke.Expression as IdentifierNameSyntax)
                .Identifier
                .Text;

            var diagnostic = Diagnostic.Create(
                Rule,
                lambda.GetLocation(),
                methodName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}
