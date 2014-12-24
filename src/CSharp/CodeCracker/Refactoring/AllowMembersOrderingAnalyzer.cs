using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;
namespace CodeCracker.Refactoring
{
    public class AllowMembersOrderingAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0035";

        internal const string Title = "Ordening member inside this type.";
        internal const string MessageFormat = "Ordening member inside this type.";
        internal const string Category = SupportedCategories.Style;

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration);
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.StructDeclaration);
        }

        private void Analyze(SyntaxNodeAnalysisContext context)
        {
            var typeDeclarationSyntax = context.Node as TypeDeclarationSyntax;

            if (typeDeclarationSyntax == null) return;

            var currentChildNodesOrder = typeDeclarationSyntax.ChildNodes();

            if (currentChildNodesOrder.Count() > 1)
                context.ReportDiagnostic(Diagnostic.Create(Rule, typeDeclarationSyntax.GetLocation()));
        }
    }
}