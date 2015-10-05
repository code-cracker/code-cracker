using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Performance
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyFinalizerAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Remove Empty Finalizers";
        internal const string MessageFormat = "Remove Empty Finalizers";
        internal const string Category = SupportedCategories.Performance;
        const string Description = "An empty finalizer will stop your object from being collected immediately by the "
            + "Garbage Collector when no longer used."
            + "It will instead be placed in the finalizer queue needlessly using resources.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.EmptyFinalizer.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            customTags: WellKnownDiagnosticTags.Unnecessary,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.EmptyFinalizer));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.DestructorDeclaration);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var finalizer = (DestructorDeclarationSyntax)context.Node;
            var body = finalizer.Body;

            if (body == null) return;

            if (body.DescendantNodes().Any(n => !n.IsKind(SyntaxKind.SingleLineCommentTrivia | SyntaxKind.MultiLineCommentTrivia))) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, finalizer.GetLocation(), finalizer.Identifier.Text));
        }
    }
}