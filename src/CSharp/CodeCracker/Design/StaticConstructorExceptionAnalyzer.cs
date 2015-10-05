using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Design
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class StaticConstructorExceptionAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Don't throw exception inside static constructors.";
        internal const string MessageFormat = "Don't throw exception inside static constructors.";
        internal const string Category = SupportedCategories.Design;
        const string Description = "Static constructor are called before the first time a class is used but the "
            + "caller doesn't control when exactly.\r\n"
            + "Exception thrown in this context force callers to use 'try' block around any useage of the class "
            + "and should be avoided.";

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.StaticConstructorException.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.StaticConstructorException));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) =>
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ConstructorDeclaration);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;
            var ctor = (ConstructorDeclarationSyntax)context.Node;

            if (!ctor.Modifiers.Any(SyntaxKind.StaticKeyword)) return;

            if (ctor.Body == null) return;

            var @throw = ctor.Body.ChildNodes().OfType<ThrowStatementSyntax>().FirstOrDefault();

            if (@throw == null) return;

            context.ReportDiagnostic(Diagnostic.Create(Rule, @throw.GetLocation(), ctor.Identifier.Text));
        }
    }
}