using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class AbstractClassShouldNotHavePublicCtorsAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Abastract class should not have public constructors.";
        internal const string MessageFormat = "Constructor should not be public.";
        internal const string Category = SupportedCategories.Usage;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.AbstractClassShouldNotHavePublicCtors.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.AbstractClassShouldNotHavePublicCtors));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(AnalyzeNode, SyntaxKind.ConstructorDeclaration);

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var ctor = (ConstructorDeclarationSyntax)context.Node;
            if (!ctor.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword))) return;

            var @class = ctor.Ancestors().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            if (@class == null) return;
            if (!@class.Modifiers.Any(m => m.IsKind(SyntaxKind.AbstractKeyword))) return;

            var diagnostic = Diagnostic.Create(Rule, ctor.GetLocation());
            context.ReportDiagnostic(diagnostic);
        }
    }
}