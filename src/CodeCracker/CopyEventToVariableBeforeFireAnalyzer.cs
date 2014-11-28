using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CopyEventToVariableBeforeFireAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0016";
        internal const string Title = "Copy Event To Variable Before Fire";
        internal const string MessageFormat = "Copy the '{0}' event to a variable before fire it.";
        internal const string Category = "Warning";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

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

            if (typeInfo.ConvertedType.BaseType.Name != typeof(MulticastDelegate).Name || symbol is ILocalSymbol) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), ((IdentifierNameSyntax)invocation.Expression).Identifier.Text));
        }
    }
}