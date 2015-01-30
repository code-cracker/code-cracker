using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RethrowExceptionAnalyzer : DiagnosticAnalyzer
    {
        private static readonly string diagnosticId = DiagnosticId.RethrowException.ToDiagnosticId();
        internal const string Title = "Your throw does nothing";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Naming;
        const string Description = "Throwing the same exception as passed to the 'catch' block lose the original "
            + "stack trace and will make debugging this exception a lot more difficult.\r\n"
            + "The correct way to rethrow an exception without changing it is by using 'throw' without any parameter.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            diagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: false,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(diagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ThrowStatement);

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var throwStatement = (ThrowStatementSyntax)context.Node;
            var ident = throwStatement.Expression as IdentifierNameSyntax;
            if (ident == null) return;
            var exSymbol = context.SemanticModel.GetSymbolInfo(ident).Symbol as ILocalSymbol;
            if (exSymbol == null) return;
            var catchClause = context.Node.Parent.AncestorsAndSelf().OfType<CatchClauseSyntax>().FirstOrDefault();
            if (catchClause == null) return;
            var catchExSymbol = context.SemanticModel.GetDeclaredSymbol(catchClause.Declaration);
            if (!catchExSymbol.Equals(exSymbol)) return;
            var diagnostic = Diagnostic.Create(Rule, throwStatement.GetLocation(), "Don't throw the same exception you caught, you lose the original stack trace.");
            context.ReportDiagnostic(diagnostic);
        }
    }
}