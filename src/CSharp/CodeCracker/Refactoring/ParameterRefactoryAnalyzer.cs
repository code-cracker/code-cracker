using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ParameterRefactoryAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "You should use a class";
        internal const string MessageFormat = "When the method has more than three parameters, use new class.";
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ParameterRefactory.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ParameterRefactory));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);
        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.MethodDeclaration);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
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