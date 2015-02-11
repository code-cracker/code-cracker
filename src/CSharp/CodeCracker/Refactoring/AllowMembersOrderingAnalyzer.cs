using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AllowMembersOrderingAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Ordering member inside this type.";
        internal const string MessageFormat = "Ordering member inside this type.";
        internal const string Category = SupportedCategories.Refactoring;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.AllowMembersOrdering.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId.AllowMembersOrdering));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var typeDeclarationSyntax = context.Node as TypeDeclarationSyntax;

            if (typeDeclarationSyntax == null) return;

            var currentChildNodesOrder = typeDeclarationSyntax.ChildNodes();

            if (currentChildNodesOrder.Count() > 1)
                context.ReportDiagnostic(Diagnostic.Create(Rule, typeDeclarationSyntax.Identifier.GetLocation()));
        }
    }
}