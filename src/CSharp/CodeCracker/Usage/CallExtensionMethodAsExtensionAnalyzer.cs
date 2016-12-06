using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CallExtensionMethodAsExtensionAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Call Extension Method As Extension";
        internal const string MessageFormat = "Do not call '{0}' method of class '{1}' as a static method";
        internal const string Category = SupportedCategories.Usage;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.CallExtensionMethodAsExtension.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.CallExtensionMethodAsExtension));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterCompilationStartAction(AnalyzeCompilation);

        private static void AnalyzeCompilation(CompilationStartAnalysisContext compilationContext)
        {
            var compilation = compilationContext.Compilation;
            compilationContext.RegisterSyntaxNodeAction(context => AnalyzeInvocation(context, compilation), SyntaxKind.InvocationExpression);
        }

        private static readonly SyntaxAnnotation introduceExtensionMethodAnnotation = new SyntaxAnnotation("CallExtensionMethodAsExtensionAnalyzerIntroduceExtensionMethod");

        private static void AnalyzeInvocation(SyntaxNodeAnalysisContext context, Compilation compilation)
        {
            if (context.IsGenerated()) return;
            var methodInvokeSyntax = context.Node as InvocationExpressionSyntax;
            var childNodes = methodInvokeSyntax.ChildNodes();
            var methodCaller = childNodes.OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            if (methodCaller == null) return;
            var argumentsCount = CountArguments(childNodes);
            if (argumentsCount == 0) return;
            var classSymbol = GetCallerClassSymbol(context.SemanticModel, methodCaller.Expression);
            if (classSymbol == null || !classSymbol.MightContainExtensionMethods) return;
            var methodSymbol = GetCallerMethodSymbol(context.SemanticModel, methodCaller.Name, argumentsCount);
            if (methodSymbol == null || !methodSymbol.IsExtensionMethod) return;
            if (ContainsDynamicArgument(context.SemanticModel, childNodes)) return;
            if (IsSelectingADifferentMethod(childNodes, methodCaller.Name, context.Node.SyntaxTree, methodSymbol, methodInvokeSyntax, compilation)) return;
            context.ReportDiagnostic(Diagnostic.Create(Rule, methodCaller.GetLocation(), methodSymbol.Name, classSymbol.Name));
        }

        private static bool IsSelectingADifferentMethod(IEnumerable<SyntaxNode> childNodes, SimpleNameSyntax methodName, SyntaxTree tree, IMethodSymbol methodSymbol, ExpressionSyntax invocationExpression, Compilation compilation)
        {
            var parameterExpressions = GetParameterExpressions(childNodes);
            var firstArgument = parameterExpressions.FirstOrDefault();
            var argumentList = CreateArgumentListSyntaxFrom(parameterExpressions.Skip(1));
            var newInvocationStatement = CreateInvocationExpression(firstArgument, methodName, argumentList)
                .WithAdditionalAnnotations(introduceExtensionMethodAnnotation);
            var extensionMethodNamespaceUsingDirective = SyntaxFactory.UsingDirective(methodSymbol.ContainingNamespace.ToNameSyntax());
            var speculativeRootWithExtensionMethod = tree.GetCompilationUnitRoot()
                .ReplaceNode(invocationExpression, newInvocationStatement)
                .AddUsings(extensionMethodNamespaceUsingDirective);
            var speculativeTree = speculativeRootWithExtensionMethod.SyntaxTree;
            var speculativeTreeOptions = (CSharpParseOptions)speculativeTree.Options;
            var speculativeTreeWithCorrectLanguageVersion = speculativeTree.WithRootAndOptions(speculativeRootWithExtensionMethod, speculativeTreeOptions.WithLanguageVersion(((CSharpParseOptions)tree.Options).LanguageVersion));
            var speculativeModel = compilation.ReplaceSyntaxTree(tree, speculativeTreeWithCorrectLanguageVersion)
                .GetSemanticModel(speculativeTreeWithCorrectLanguageVersion);
            var speculativeInvocationStatement = speculativeTreeWithCorrectLanguageVersion.GetCompilationUnitRoot().GetAnnotatedNodes(introduceExtensionMethodAnnotation).Single() as InvocationExpressionSyntax;
            var speculativeExtensionMethodSymbol = speculativeModel.GetSymbolInfo(speculativeInvocationStatement.Expression).Symbol as IMethodSymbol;
            var speculativeNonExtensionFormOfTheMethodSymbol = speculativeExtensionMethodSymbol?.GetConstructedReducedFrom();
            return speculativeNonExtensionFormOfTheMethodSymbol == null || speculativeNonExtensionFormOfTheMethodSymbol.ToString() != methodSymbol.ToString();//can't compare equality, as speculative symbol might be different
        }

        private static int CountArguments(IEnumerable<SyntaxNode> childNodes) =>
            childNodes.OfType<ArgumentListSyntax>().Select(s => s.Arguments.Count).FirstOrDefault();

        private static IMethodSymbol GetCallerMethodSymbol(SemanticModel semanticModel, SimpleNameSyntax name, int argumentsCount)
        {
            var symbolInfo = semanticModel.GetSymbolInfo(name);
            return symbolInfo.Symbol as IMethodSymbol ??
                    symbolInfo
                        .CandidateSymbols
                        .OfType<IMethodSymbol>()
                        .FirstOrDefault(s => s.Parameters.Length == argumentsCount + 1);
        }

        private static INamedTypeSymbol GetCallerClassSymbol(SemanticModel semanticModel, ExpressionSyntax expression) =>
            semanticModel.GetSymbolInfo(expression).Symbol as INamedTypeSymbol;

        private static bool ContainsDynamicArgument(SemanticModel sm, IEnumerable<SyntaxNode> childNodes) =>
            childNodes
                .OfType<ArgumentListSyntax>()
                .SelectMany(s => s.Arguments)
                .Any(a => sm.GetTypeInfo(a.Expression).Type?.Name == "dynamic");

        public static IEnumerable<ExpressionSyntax> GetParameterExpressions(IEnumerable<SyntaxNode> childNodes) =>
            childNodes.OfType<ArgumentListSyntax>().SelectMany(s => s.Arguments).Select(s => s.Expression);

        public static ArgumentListSyntax CreateArgumentListSyntaxFrom(IEnumerable<ExpressionSyntax> expressions) =>
            SyntaxFactory.ArgumentList().AddArguments(expressions.Select(s => SyntaxFactory.Argument(s)).ToArray());

        public static InvocationExpressionSyntax CreateInvocationExpression(ExpressionSyntax sourceExpression, SimpleNameSyntax methodName, ArgumentListSyntax argumentList) =>
            SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                SyntaxKind.SimpleMemberAccessExpression,
                sourceExpression,
                methodName),
                argumentList);
    }
}