using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Refactoring
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AllowMembersOrderingAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Ordering member inside this type.";
        internal const string MessageFormat = "Ordering member inside this type.";
        internal const string Category = SupportedCategories.Refactoring;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.AllowMembersOrdering.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Hidden,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.AllowMembersOrdering));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyze, SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration);

        private static void Analyze(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var typeDeclarationSyntax = context.Node as TypeDeclarationSyntax;
            if (!CanOrder(typeDeclarationSyntax)) return;
            context.ReportDiagnostic(Diagnostic.Create(Rule, typeDeclarationSyntax.Identifier.GetLocation()));
        }

        private static bool CanOrder(TypeDeclarationSyntax typeDeclarationSyntax) => typeDeclarationSyntax != null && typeDeclarationSyntax.Members.Count > 1;
    }
}