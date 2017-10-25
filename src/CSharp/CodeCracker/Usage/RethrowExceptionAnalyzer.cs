using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RethrowExceptionAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Your throw does nothing";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Naming;
        const string Description = "If a exception is caught and then thrown again the original stack trace will be lost. "
            + "Instead it is best to throw the exception without using any parameters.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RethrowException.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RethrowException));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ThrowStatement);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var throwStatement = (ThrowStatementSyntax)context.Node;
            var ident = throwStatement.Expression as IdentifierNameSyntax;
            if (ident == null) return;
            var exSymbol = context.SemanticModel.GetSymbolInfo(ident).Symbol as ILocalSymbol;
            if (exSymbol == null) return;
            var catchClause = context.Node.Parent.AncestorsAndSelf().OfType<CatchClauseSyntax>().FirstOrDefault();
            if (catchClause == null) return;
            var catchExSymbol = context.SemanticModel.GetDeclaredSymbol(catchClause.Declaration);
            if (!catchExSymbol.Equals(exSymbol)) return;
            var diagnostic = Diagnostic.Create(Rule, throwStatement.GetLocation(), "Throwing the same exception that was caught will lose the original stack trace.");
            context.ReportDiagnostic(diagnostic);
        }
    }
}
