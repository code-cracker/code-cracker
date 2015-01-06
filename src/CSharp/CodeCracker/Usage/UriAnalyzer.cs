using System;
using System.Collections.Immutable;
using CodeCracker.Usage.MethodAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeCracker.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UriAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CC0063";
        internal const string Title = "Your Uri syntax is wrong.";
        internal const string MessageFormat = "'{0}'";
        internal const string Category = SupportedCategories.Usage;

        private const string Description = "This diagnostic checks the Uri string and triggers if the parsing fail "
                                           + "by throwing an exception.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLink: HelpLink.ForDiagnostic(DiagnosticId));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context)
        {
            context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ObjectCreationExpression);
        }

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var mainConstrutor = new MethodInformation(
                "Uri",
                "System.Uri.Uri(string)",
                args => { new Uri(args[0].ToString()); }
            );
            var constructorWithUriKind = new MethodInformation(
                "Uri",
                "System.Uri.Uri(string, System.UriKind)",
                args => { new Uri(args[0].ToString(), (UriKind)args[1]); }
            );

            var checker = new MethodChecker(context, Rule);
            checker.AnalyzeConstrutor(mainConstrutor);
            checker.AnalyzeConstrutor(constructorWithUriKind);
        }
    }
}