using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveWhereWhenItIsPossibleAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "You should remove the 'Where' invocation when it is possible.";
        internal const string MessageFormat = "You can remove 'Where' moving the predicate to '{0}'.";
        internal const string Category = SupportedCategories.Performance;
        const string Description = "When a linq operator support a predicate parameter it should be used instead of "
            + "using 'Where' followed by the operator";

        static readonly string[] supportedMethods = new[] {
            "First",
            "FirstOrDefault",
            "Last",
            "LastOrDefault",
            "Any",
            "Single",
            "SingleOrDefault",
            "Count"
        };
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RemoveWhereWhenItIsPossible.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveWhereWhenItIsPossible));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var whereInvoke = (InvocationExpressionSyntax)context.Node;
            if (GetNameOfTheInvokedMethod(whereInvoke) != "Where") return;

            var nextMethodInvoke = whereInvoke.Parent.
                FirstAncestorOrSelf<InvocationExpressionSyntax>();

            var candidate = GetNameOfTheInvokedMethod(nextMethodInvoke);
            if (!supportedMethods.Contains(candidate)) return;

            if (nextMethodInvoke.ArgumentList.Arguments.Any()) return;
            var properties = new Dictionary<string, string> { { "methodName", candidate } }.ToImmutableDictionary();
            var diagnostic = Diagnostic.Create(Rule, GetNameExpressionOfTheInvokedMethod(whereInvoke).GetLocation(), properties, candidate);
            context.ReportDiagnostic(diagnostic);
        }

        internal static string GetNameOfTheInvokedMethod(InvocationExpressionSyntax invoke)
        {
            if (invoke == null) return null;

            var memberAccess = invoke.ChildNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .FirstOrDefault();

            return GetNameExpressionOfTheInvokedMethod(invoke)?.ToString();
        }

        internal static SimpleNameSyntax GetNameExpressionOfTheInvokedMethod(InvocationExpressionSyntax invoke)
        {
            if (invoke == null) return null;

            var memberAccess = invoke.ChildNodes()
                .OfType<MemberAccessExpressionSyntax>()
                .FirstOrDefault();

            return memberAccess?.Name;
        }
    }
}