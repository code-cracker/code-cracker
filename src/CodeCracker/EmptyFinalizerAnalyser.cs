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
        public const string DiagnosticId = "CC0025";
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
            var body = finalizer.Body;

            if (body == null) return;

            if (body.DescendantNodes().Count(n => !n.IsKind(SyntaxKind.SingleLineCommentTrivia | SyntaxKind.MultiLineCommentTrivia)) > 0) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, finalizer.GetLocation(), finalizer.Identifier.Text));
        }
    }
}