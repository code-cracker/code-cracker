using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ChangeAnyToAllAnalyzer : DiagnosticAnalyzer
    {
        private const string speculativeAnnotationDescription = "ChangeAnyToAllAnalyzer_speculativeAnnotation";
        private static readonly SyntaxAnnotation speculativeAnnotation = new SyntaxAnnotation(speculativeAnnotationDescription);
        internal const string MessageAny = "Change Any to All";
        internal const string MessageAll = "Change All to Any";
        internal const string TitleAny = MessageAny;
        internal const string TitleAll = MessageAll;
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor RuleAny = new DiagnosticDescriptor(
            DiagnosticId.ChangeAnyToAll.ToDiagnosticId(),
            TitleAny,
            MessageAny,
            Category,
            SeverityConfigurations.CurrentCS[DiagnosticId.ChangeAnyToAll],
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ChangeAnyToAll));
        internal static readonly DiagnosticDescriptor RuleAll = new DiagnosticDescriptor(
            DiagnosticId.ChangeAllToAny.ToDiagnosticId(),
            TitleAll,
            MessageAll,
            Category,
            SeverityConfigurations.CurrentCS[DiagnosticId.ChangeAllToAny],
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ChangeAllToAny));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(RuleAny, RuleAll);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);

        public static readonly SimpleNameSyntax allName = (SimpleNameSyntax)SyntaxFactory.ParseName("All");
        public static readonly SimpleNameSyntax anyName = (SimpleNameSyntax)SyntaxFactory.ParseName("Any");

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (invocation.Parent?.IsKind(SyntaxKind.ExpressionStatement) ?? true) return;
            var diagnosticToRaise = GetCorrespondingDiagnostic(context.SemanticModel, invocation);
            if (diagnosticToRaise == null) return;
            var diagnostic = Diagnostic.Create(diagnosticToRaise, GetName(invocation).GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static DiagnosticDescriptor GetCorrespondingDiagnostic(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
        {
            var methodName = GetName(invocation);
            var methodNameText = methodName?.ToString();
            var nameToCheck = methodNameText == "Any" ? allName : methodNameText == "All" ? anyName : null;
            if (nameToCheck == null) return null;
            var methodSymbol = semanticModel.GetSymbolInfo(invocation.Expression).Symbol as IMethodSymbol;
            if (methodSymbol?.Parameters.Length != 1) return null;
            if (!IsLambdaWithoutBody(invocation)) return null;
            if (!OtherMethodExists(invocation, nameToCheck, semanticModel)) return null;
            return methodNameText == "Any" ? RuleAny : RuleAll;
        }

        public static SimpleNameSyntax GetName(InvocationExpressionSyntax invocation)
        {
            SimpleNameSyntax methodName = null;
            if (invocation.Expression.IsKind(SyntaxKind.MemberBindingExpression))
                methodName = ((MemberBindingExpressionSyntax)invocation.Expression).Name;
            else if (invocation.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression))
                methodName = ((MemberAccessExpressionSyntax)invocation.Expression).Name;
            return methodName;
        }

        private static bool IsLambdaWithoutBody(InvocationExpressionSyntax invocation)
        {
            var arg = invocation.ArgumentList?.Arguments.First();
            var lambda = arg.Expression as LambdaExpressionSyntax;
            if (lambda == null) return false;
            return !(lambda.Body is BlockSyntax);
        }

        private static bool OtherMethodExists(InvocationExpressionSyntax invocation, SimpleNameSyntax nameToCheck, SemanticModel semanticModel)
        {
            var otherExpression = CreateExpressionWithNewName(invocation, nameToCheck);
            var statement = invocation.FirstAncestorOrSelfThatIsAStatement();
            SemanticModel speculativeModel;
            if (statement != null)
            {
                var otherStatement = statement.ReplaceNode(invocation.Expression, otherExpression);
                if (!semanticModel.TryGetSpeculativeSemanticModel(statement.SpanStart, otherStatement, out speculativeModel)) return false;
            }
            else
            {
                var arrow = (ArrowExpressionClauseSyntax)invocation.FirstAncestorOfKind(SyntaxKind.ArrowExpressionClause);
                if (arrow == null) return false;
                var otherArrow = arrow.ReplaceNode(invocation.Expression, otherExpression);
                if (!semanticModel.TryGetSpeculativeSemanticModel(arrow.SpanStart, otherArrow, out speculativeModel)) return false;
            }
            var symbol = speculativeModel.GetSymbolInfo(speculativeModel.SyntaxTree.GetRoot().GetAnnotatedNodes(speculativeAnnotationDescription).First()).Symbol;
            return symbol != null;
        }

        public static ExpressionSyntax CreateExpressionWithNewName(InvocationExpressionSyntax invocation, SimpleNameSyntax nameToCheck)
        {
            var otherExpression = invocation.Expression.IsKind(SyntaxKind.MemberBindingExpression)
                ? (ExpressionSyntax)((MemberBindingExpressionSyntax)invocation.Expression).WithName(nameToCheck).WithAdditionalAnnotations(speculativeAnnotation)
                //avoid this, already checked before: if (invocation.Expression.IsKind(SyntaxKind.SimpleMemberAccessExpression)):
                : ((MemberAccessExpressionSyntax)invocation.Expression).WithName(nameToCheck).WithAdditionalAnnotations(speculativeAnnotation);
            return otherExpression;
        }
    }
}