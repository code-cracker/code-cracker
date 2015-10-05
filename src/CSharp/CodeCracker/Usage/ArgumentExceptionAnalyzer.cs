using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ArgumentExceptionAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Invalid argument name";
        internal const string MessageFormat = "Type argument '{0}' is not in the argument list.";
        internal const string Category = SupportedCategories.Usage;
        const string Description = "The string passed as the 'paramName' argument of ArgumentException constructor "
            + "must be the name of one of the method arguments.\r\n"
            + "It can be either specified directly or using the nameof() operator (C#6 only)";
        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ArgumentException.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ArgumentException));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var objectCreationExpression = (ObjectCreationExpressionSyntax)context.Node;

            var type = objectCreationExpression.Type;
            var typeSymbol = context.SemanticModel.GetSymbolInfo(type).Symbol as ITypeSymbol;
            if (!typeSymbol?.ToString().EndsWith("System.ArgumentException") ?? true) return;

            var argumentList = objectCreationExpression.ArgumentList as ArgumentListSyntax;
            if ((argumentList?.Arguments.Count ?? 0) < 2) return;

            var paramNameLiteral = argumentList.Arguments[1].Expression as LiteralExpressionSyntax;
            if (paramNameLiteral == null) return;

            var paramNameOpt = context.SemanticModel.GetConstantValue(paramNameLiteral);
            if (!paramNameOpt.HasValue) return;

            var paramName = paramNameOpt.Value as string;

            IList<string> parameters;
            if (IsParamNameCompatibleWithCreatingContext(objectCreationExpression, paramName, out parameters)) return;
            var props = parameters.ToImmutableDictionary(p => $"param{p}", p => p);
            var diagnostic = Diagnostic.Create(Rule, paramNameLiteral.GetLocation(), props.ToImmutableDictionary(), paramName);
            context.ReportDiagnostic(diagnostic);
        }

        private static bool IsParamNameCompatibleWithCreatingContext(SyntaxNode node, string paramName, out IList<string> parameters)
        {
            parameters = GetParameterNamesFromCreationContext(node);
            if (parameters == null) return true;
            return parameters.Contains(paramName);
        }

        private static IList<string> GetParameterNamesFromCreationContext(SyntaxNode node)
        {
            var creationContext =
                node.FirstAncestorOrSelf<SimpleLambdaExpressionSyntax>() ??
                node.FirstAncestorOrSelf<ParenthesizedLambdaExpressionSyntax>() ??
                node.FirstAncestorOrSelf<AccessorDeclarationSyntax>() ??
                (SyntaxNode)node.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>();
            return GetParameterNames(creationContext);
        }

        internal static IList<string> GetParameterNames(SyntaxNode node)
        {
            var simpleLambda = node as SimpleLambdaExpressionSyntax;
            if (simpleLambda != null)
            {
                return new[] { simpleLambda.Parameter.Identifier.ToString() };
            }
            var method = node as BaseMethodDeclarationSyntax;
            if (method != null)
            {
                var parameterList = method.ParameterList;
                return (parameterList == null)
                    ? new List<string>()
                    : parameterList.Parameters.Select(p => p.Identifier.ToString()).ToList();
            }
            var lambda = node as ParenthesizedLambdaExpressionSyntax;
            if (lambda != null)
            {
                var parameterList = lambda.ParameterList;
                return (parameterList == null)
                    ? new List<string>()
                    : parameterList.Parameters.Select(p => p.Identifier.ToString()).ToList();
            }
            var accessor = node as AccessorDeclarationSyntax;
            if (accessor != null)
            {
                var indexer = node.FirstAncestorOrSelf<IndexerDeclarationSyntax>();
                if (indexer != null)
                {
                    var result = indexer.ParameterList.Parameters.Select(p => p.Identifier.ToString());
                    if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                    {
                        result = result.Concat(new[] { "value" });
                    }
                    return result.ToList();
                }
                if (accessor.IsKind(SyntaxKind.SetAccessorDeclaration))
                {
                    return new List<string> { "value" };
                }
            }
            return null;
        }

    }
}