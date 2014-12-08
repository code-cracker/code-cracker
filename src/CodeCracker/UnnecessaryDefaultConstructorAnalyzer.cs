using System.Collections.Immutable;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UnnecessaryDefaultConstructorAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CodeCracker.UnnecessaryDefaultConstructorAnalyzer";
        internal const string Title = "Unnecessary default constructor";
        internal const string Message = "Default constructor is unnecessary.";
        internal const string Category = "Refactoring";

        public static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId,
            Title,
            Message, "Usage",
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(AnalyzeNodeAction, SyntaxKind.ConstructorDeclaration);
        }

        private void AnalyzeNodeAction(SyntaxNodeAnalysisContext syntaxNodeAnalysisContext)
        {
            var constructorDeclaration = (ConstructorDeclarationSyntax) syntaxNodeAnalysisContext.Node;
            var classDeclaretion = constructorDeclaration.Parent;
            var constructors = classDeclaretion.ChildNodes().OfType<ConstructorDeclarationSyntax>().ToList();
            var defaultConstructor = constructors.FirstOrDefault(_ => !_.ParameterList.Parameters.Any());

            if (constructorDeclaration.ParameterList.Parameters.Any()) return;

            if (!constructors.Any(_ => _.ParameterList.Parameters.Any()))
            {
                if (defaultConstructor != null)
                {
                    var diagnostic = Diagnostic.Create(Rule, defaultConstructor.GetLocation());
                    syntaxNodeAnalysisContext.ReportDiagnostic(diagnostic);
                }
            }
            else
            {
                if (defaultConstructor != null && defaultConstructor.Modifiers.Any(SyntaxKind.PrivateKeyword))
                {
                    var diagnostic = Diagnostic.Create(Rule, defaultConstructor.GetLocation());
                    syntaxNodeAnalysisContext.ReportDiagnostic(diagnostic);
                }
            }
        }
    }
}
