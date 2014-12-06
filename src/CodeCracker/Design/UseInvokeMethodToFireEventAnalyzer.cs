using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using RoslynExts.CS;

namespace CodeCracker.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseInvokeMethodToFireEventAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0031";
        internal const string Title = "Use Invoke Method To Fire Event Analyzer";
        internal const string MessageFormat = "Use ?.Invoke operator and method to fire '{0}' event.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "In C#6 an event can be invoked using the null-propagating operator (?.) and it's"
            + "invoke method to avoid throwing a NullReference exception when there is no event handler attached.";

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
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyzer, SyntaxKind.InvocationExpression);
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