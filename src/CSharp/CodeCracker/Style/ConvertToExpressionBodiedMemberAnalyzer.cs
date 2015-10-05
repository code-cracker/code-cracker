using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ConvertToExpressionBodiedMemberAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "You should use expression bodied members whenever possible.";
        internal const string MessageFormat = "Use an expression bodied member.";
        internal const string Category = SupportedCategories.Style;
        const string Description = "Usage of an expression bodied members improve readability of the code.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.ConvertToExpressionBodiedMember.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.ConvertToExpressionBodiedMember));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, AnalyzeBaseMethodNode, SyntaxKind.MethodDeclaration, SyntaxKind.OperatorDeclaration, SyntaxKind.ConversionOperatorDeclaration);
            context.RegisterSyntaxNodeAction(LanguageVersion.CSharp6, AnalyzeBasePropertyNode, SyntaxKind.IndexerDeclaration, SyntaxKind.PropertyDeclaration);
        }

        static void AnalyzeBaseMethodNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var methodDeclaration = (BaseMethodDeclarationSyntax)context.Node;
            var body = methodDeclaration.Body;
            if (body == null) return;

            if (body.Statements.Count != 1) return;
            var returnStatement = body.Statements[0] as ReturnStatementSyntax;
            if (returnStatement == null) return;

            var diagnostic = Diagnostic.Create(Rule, methodDeclaration.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }

        private static void AnalyzeBasePropertyNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var declaration = (BasePropertyDeclarationSyntax)context.Node;
            if (declaration.AccessorList == null) return;
            var accessors = declaration.AccessorList.Accessors;
            if (accessors.Count != 1) return;
            if (!accessors[0].IsKind(SyntaxKind.GetAccessorDeclaration)) return;
            var body = accessors[0].Body;

            if (body == null) return;
            if (body.Statements.Count != 1) return;

            var returnStatement = body.Statements[0] as ReturnStatementSyntax;
            if (returnStatement == null) return;
            var diagnostic = Diagnostic.Create(Rule, declaration.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}