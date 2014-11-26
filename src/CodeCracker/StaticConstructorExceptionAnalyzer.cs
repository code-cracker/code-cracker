using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StaticConstructorExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0024";
        internal const string Title = "Don't throw exception inside static constructors.";
        internal const string MessageFormat = "Don't throw exception inside static constructors.";
        internal const string Category = "Usage";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ConstructorDeclaration);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var ctor = (ConstructorDeclarationSyntax)context.Node;

            if (!ctor.Modifiers.Any(SyntaxKind.StaticKeyword)) return;

            if (ctor.Body == null) return;

            var @throw = ctor.Body.ChildNodes().OfType<ThrowStatementSyntax>().FirstOrDefault();

            if (@throw == null) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, @throw.GetLocation(), ctor.Identifier.Text));
        }
    }
}