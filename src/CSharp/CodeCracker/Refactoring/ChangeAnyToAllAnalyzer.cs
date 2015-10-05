using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ChangeAnyToAllAnalyzer : DiagnosticAnalyzer
    {
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
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ChangeAnyToAll));
        internal static readonly DiagnosticDescriptor RuleAll = new DiagnosticDescriptor(
            DiagnosticId.ChangeAllToAny.ToDiagnosticId(),
            TitleAll,
            MessageAll,
            Category,
            DiagnosticSeverity.Hidden,
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
            var diagnostic = Diagnostic.Create(diagnosticToRaise, ((MemberAccessExpressionSyntax)invocation.Expression).Name.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static DiagnosticDescriptor GetCorrespondingDiagnostic(SemanticModel semanticModel, InvocationExpressionSyntax invocation)
        {
            var methodName = (invocation?.Expression as MemberAccessExpressionSyntax)?.Name?.ToString();
            var nameToCheck = methodName == "Any" ? allName : methodName == "All" ? anyName : null;
            if (nameToCheck == null) return null;
            var invocationSymbol = semanticModel.GetSymbolInfo(invocation).Symbol as IMethodSymbol;
            if (invocationSymbol?.Parameters.Length != 1) return null;
            if (!IsLambdaWithoutBody(invocation)) return null;
            var otherInvocation = invocation.WithExpression(((MemberAccessExpressionSyntax)invocation.Expression).WithName(nameToCheck));
            var otherInvocationSymbol = semanticModel.GetSpeculativeSymbolInfo(invocation.SpanStart, otherInvocation, SpeculativeBindingOption.BindAsExpression);
            if (otherInvocationSymbol.Symbol == null) return null;
            if (methodName == "Any")
                return RuleAny;
            return RuleAll;
        }

        private static bool IsLambdaWithoutBody(InvocationExpressionSyntax invocation)
        {
            var arg = invocation.ArgumentList?.Arguments.First();
            var lambda = arg.Expression as LambdaExpressionSyntax;
            if (lambda == null) return false;
            return !(lambda.Body is BlockSyntax);
        }
    }
}