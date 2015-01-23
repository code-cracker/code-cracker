using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CallExtensionMethodAsExtensionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0026";
        internal const string Title = "Call Extension Method As Extension";
        internal const string MessageFormat = "Do not call '{0}' method of class '{1}' as a static method";
        internal const string Category = SupportedCategories.Usage;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: false,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.InvocationExpression);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var methodInvokeSyntax = context.Node as InvocationExpressionSyntax;

            var childNodes = methodInvokeSyntax.ChildNodes();

            var methodCaller = childNodes
                                .OfType<MemberAccessExpressionSyntax>()
                                .FirstOrDefault();

            if (methodCaller == null) return;

            var argumentsCount = CountArguments(childNodes);

            var classSymbol = GetCallerClassSymbol(context.SemanticModel, methodCaller.Expression);
            if (classSymbol == null || !classSymbol.MightContainExtensionMethods) return;

            var methodSymbol = GetCallerMethodSymbol(context.SemanticModel, methodCaller.Name, argumentsCount);
            if (methodSymbol == null || !methodSymbol.IsExtensionMethod) return;

            context.ReportDiagnostic(
                Diagnostic.Create(
                    Rule,
                    methodCaller.GetLocation(),
                    methodSymbol.Name,
                    classSymbol.Name
                ));
        }

        private static int CountArguments(IEnumerable<SyntaxNode> childNodes)
        {
            return childNodes
                    .OfType<ArgumentListSyntax>()
                    .Select(s => s.Arguments.Count)
                    .FirstOrDefault();
        }

        private IMethodSymbol GetCallerMethodSymbol(SemanticModel semanticModel, SimpleNameSyntax name, int argumentsCount)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(name);

            return symbolInfo.Symbol as IMethodSymbol ??
                    symbolInfo
                        .CandidateSymbols
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(s => s.Parameters.Count() == argumentsCount + 1);
        }

        private INamedTypeSymbol GetCallerClassSymbol(SemanticModel semanticModel, ExpressionSyntax expression)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(expression);
            return symbolInfo.Symbol as INamedTypeSymbol;
        }
    }
}