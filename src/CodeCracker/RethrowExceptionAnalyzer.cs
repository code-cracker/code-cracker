using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RethrowExceptionAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.RethrowExceptionAnalyzer ";
        internal const string Title = "Your throw does nothing";
        internal const string MessageFormat = "{0}";
        internal const string Category = "Syntax";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get { return ImmutableArray.Create(Rule); } }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ThrowStatement);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var throwStatement = (ThrowStatementSyntax)context.Node;
            var ident = throwStatement.Expression as IdentifierNameSyntax;
            if (ident == null) return;
            var exSymbol = context.SemanticModel.GetSymbolInfo(ident).Symbol as ILocalSymbol;
            if (exSymbol == null) return;
            var catchClause = context.Node.Parent.AncestorsAndSelf().OfType<CatchClauseSyntax>().First();
            var catchExSymbol = context.SemanticModel.GetDeclaredSymbol(catchClause.Declaration);
            if (!catchExSymbol.Equals(exSymbol)) return;
            var diagnostic = Diagnostic.Create(Rule, throwStatement.GetLocation(), "Don't throw the same exception you caught, you lose the original stack trace.");
            context.ReportDiagnostic(diagnostic);
        }
    }
}