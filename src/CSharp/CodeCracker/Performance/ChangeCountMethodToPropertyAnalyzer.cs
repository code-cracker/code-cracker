using Microsoft.CodeAnalysis;
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
    public class ChangeCountMethodToPropertyAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.ChangeCountMethodToPropertyAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.ChangeCountMethodToPropertyAnalyzer_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.ChangeCountMethodToPropertyAnalyzer_Description), Resources.ResourceManager, typeof(Resources));
        internal const string Category = SupportedCategories.Performance;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ChangeCountMethodToProperty.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ChangeCountMethodToProperty));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.InvocationExpression);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var invocation = (context.Node as InvocationExpressionSyntax);
            if (invocation.ArgumentList?.Arguments.Count != 0) return;
            var memberExpression = invocation.Expression as MemberAccessExpressionSyntax;
            if (memberExpression?.Name?.ToString() != "Count") return;

            var checkProperty = invocation.WithExpression(memberExpression.WithName((SimpleNameSyntax)SyntaxFactory.ParseName("Count"))).Expression;
            var checkPropertySymbol = context.SemanticModel.GetSpeculativeSymbolInfo(invocation.SpanStart, checkProperty, SpeculativeBindingOption.BindAsExpression);
            if (checkPropertySymbol.Symbol == null) return;

            var diagnostic = Diagnostic.Create(Rule, invocation.GetLocation(), "");
            context.ReportDiagnostic(diagnostic);
        }
    }
}