using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UseInvokeMethodToFireEventAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Use Invoke Method To Fire Event Analyzer";
        internal const string MessageFormat = "Use ?.Invoke operator and method to fire '{0}' event.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "In C#6 an event can be invoked using the null-propagating operator (?.) and it's"
            + "invoke method to avoid throwing a NullReference exception when there is no event handler attached.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.UseInvokeMethodToFireEvent));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, Analyzer, SyntaxKind.InvocationExpression);

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var identifier = invocation.Expression as IdentifierNameSyntax;
            if (identifier == null) return;
            var typeInfo = context.SemanticModel.GetTypeInfo(identifier, context.CancellationToken);

            if (typeInfo.ConvertedType?.BaseType == null) return;

            var symbol = context.SemanticModel.GetSymbolInfo(identifier).Symbol;

            if (typeInfo.ConvertedType.BaseType.Name != typeof(MulticastDelegate).Name || symbol is ILocalSymbol) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, invocation.GetLocation(), identifier.Identifier.Text));
        }
    }
}