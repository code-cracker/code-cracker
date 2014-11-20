using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class EmptyFinalizerAnalyser : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.EmptyFinalizerAnalyser";
        internal const string Title = "Remove Empty Finalizers";
        internal const string MessageFormat = "Remove Empty Finalizers";
        internal const string Category = "Performance";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.DestructorDeclaration);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var finalizer = (DestructorDeclarationSyntax)context.Node;

            if (finalizer.Body.DescendantNodes()?.Count() > 0) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, finalizer.GetLocation(), finalizer.Identifier.Text));
        }
    }
}