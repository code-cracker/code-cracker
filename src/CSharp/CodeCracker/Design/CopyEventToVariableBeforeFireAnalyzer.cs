using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace CodeCracker.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CopyEventToVariableBeforeFireAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0016";
        internal const string Title = "Copy Event To Variable Before Fire";
        internal const string MessageFormat = "Copy the '{0}' event to a variable before fire it.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "Events should always be checked for null before being invoked.\r\n"
            + "As in a multi-threading context it is possible for an event to be unsuscribed between "
            + "the moment where it is checked to be non-null and the moment it is raised the event must "
            + "be copied to a temporary variable before the check.";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var typeInfo = context.SemanticModel.GetTypeInfo(invocation.Expression, context.CancellationToken);

            if (typeInfo.ConvertedType?.BaseType == null) return;

            var symbol = context.SemanticModel.GetSymbolInfo(invocation.Expression).Symbol;

            if (typeInfo.ConvertedType.BaseType.Name != typeof(MulticastDelegate).Name || symbol is ILocalSymbol || symbol is IParameterSymbol) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), ((IdentifierNameSyntax)invocation.Expression).Identifier.Text));
        }
    }
}