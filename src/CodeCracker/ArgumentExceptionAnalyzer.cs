using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class WrongArgumentNameAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.ArgumentExceptionAnalyzer";
        internal const string Title = "Invalid argument name";
        internal const string MessageFormat = "Type argument '{0}' is not in the argument list.";
        internal const string Category = "Naming";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Error, isEnabledByDefault: true);

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

            if (!typeSymbol?.ToString().EndsWith("System.ArgumentException") ?? true)
            {
                return;
            }

            var argumentList = objectCreationExpression.ArgumentList as ArgumentListSyntax;

            if ((argumentList?.Arguments.Count ?? 0) < 2)
            {
                return;
            }

            var paramNameLiteral = argumentList.Arguments[1].Expression as LiteralExpressionSyntax;
            if (paramNameLiteral == null)
            {
                return;
            }

            var paramNameOpt = context.SemanticModel.GetConstantValue(paramNameLiteral);
            if (!paramNameOpt.HasValue)
            {
                return;
            }

            var paramName = paramNameOpt.Value as string;

            var ancestorMethod = objectCreationExpression.FirstAncestorOrSelf<MethodDeclarationSyntax>();

            var parameters = ancestorMethod.ParameterList.Parameters.Select(p => p.Identifier.ToString());
            if (!parameters.Any(p => p == paramName))
            {
                var diagnostic = Diagnostic.Create(Rule, paramNameLiteral.GetLocation(), paramName);
                context.ReportDiagnostic(diagnostic);
            }

        }
    }
}