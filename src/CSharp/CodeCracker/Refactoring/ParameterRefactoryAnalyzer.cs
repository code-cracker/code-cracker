using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
using System;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ParameterRefactoryAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0020";
        internal const string Title = "You should using 'new class'";
        internal const string MessageFormat = "When the method has more than three parameters, use new class.";
        internal const string Category = "Syntax";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);
        }

        private void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            var method = (MethodDeclarationSyntax)context.Node;

            if (method.Modifiers.Any(SyntaxKind.ExternKeyword)) return;

            var contentParameter = method.ParameterList;

            if (!contentParameter.Parameters.Any() || contentParameter.Parameters.Count <= 3) return;

            if (contentParameter.Parameters.SelectMany(parameter => parameter.Modifiers)
                    .Any(modifier => modifier.IsKind(SyntaxKind.RefKeyword) ||
                                     modifier.IsKind(SyntaxKind.OutKeyword) ||
                                     modifier.IsKind(SyntaxKind.ThisKeyword) ||
                                     modifier.IsKind(SyntaxKind.ParamsKeyword))) return;

            if (method.Body?.ChildNodes().Count() > 0) return;


            var diagnostic = Diagnostic.Create(Rule, contentParameter.GetLocation());

            context.ReportDiagnostic(diagnostic);
        }
    }
}

