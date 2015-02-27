using CodeCracker.CSharp.Usage.MethodAnalyzers;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;

namespace CodeCracker.CSharp.Usage
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class UriAnalyzer : DiagnosticAnalyzer
    {
        internal const string Title = "Your Uri syntax is wrong.";
        internal const string MessageFormat = "{0}";
        internal const string Category = SupportedCategories.Usage;

        private const string Description = "This diagnostic checks the Uri string and triggers if the parsing fail "
                                           + "by throwing an exception.";

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.Uri.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.Uri));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics
        {
            get { return ImmutableArray.Create(Rule); }
        }

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.ObjectCreationExpression);

        private void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var mainConstrutor = new MethodInformation(
                "Uri",
                "System.Uri.Uri(string)",
                args =>
                {
                    {
                        if (args[0] == null)
                        {
                            return;
                        }
                        new Uri(args[0].ToString());
                    }
                }
            );
            var constructorWithUriKind = new MethodInformation(
                "Uri",
                "System.Uri.Uri(string, System.UriKind)",
                args =>
                {
                    if (args[0] == null)
                    {
                        return;
                    }
                    new Uri(args[0].ToString(), (UriKind)args[1]);
                }
            );

            var checker = new MethodChecker(context, Rule);
            checker.AnalyzeConstrutor(mainConstrutor);
            checker.AnalyzeConstrutor(constructorWithUriKind);
        }
    }
}