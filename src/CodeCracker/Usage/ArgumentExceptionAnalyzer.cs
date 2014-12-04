using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ArgumentExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0002";
        internal const string Title = "Invalid argument name";
        internal const string MessageFormat = "Type argument '{0}' is not in the argument list.";
        internal const string Category = SupportedCategories.Naming;
        const string Description = "The string passed as the 'paramName' argument of ArgumentException constructor "
            + "must be the name of one of the method arguments.\r\n"
            + "It can be either specified directly or using the nameof() operator (C#6 only)";
        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description:Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ObjectCreationExpression);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
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

            var parameterList = objectCreationExpression.FirstAncestorOrSelf<BaseMethodDeclarationSyntax>()?.ParameterList;
            if (parameterList == null) return;

            var parameters = parameterList.Parameters.Select(p => p.Identifier.ToString());
            if (parameters.All(p => p == paramName)) return;
            var diagnostic = Diagnostic.Create(Rule, paramNameLiteral.GetLocation(), paramName);
            context.ReportDiagnostic(diagnostic);
        }
    }
}