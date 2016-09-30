using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System;
using System.Collections.Immutable;
using System.Linq;

namespace CodeCracker.CSharp.Style
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class RemoveAtFromVariablesAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.RemoveAtFromVariablesAnalyzer_Title), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.RemoveAtFromVariablesAnalyzer_MessageFormat), Resources.ResourceManager, typeof(Resources));
        internal static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.RemoveAtFromVariablesAnalyzer_Description), Resources.ResourceManager, typeof(Resources));

        internal const string Category = SupportedCategories.Style;

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords.ToDiagnosticId(),
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            description: Description,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction
            (
                AnalyzeNode,
                SyntaxKind.VariableDeclaration,
                SyntaxKind.FieldDeclaration,
                SyntaxKind.Parameter
            );

        private static void AnalyzeNode(SyntaxNodeAnalysisContext context)
        {
            if (context.IsGenerated()) return;

            var nodes = context.Node.SyntaxTree.GetRoot().DescendantNodes();

            foreach (var node in nodes)
            {
                foreach (var token in node.ChildTokens())
                {
                    if (token.Text.StartsWith(@"@"))
                    {
                        var identifier = token.ValueText;
                        if (identifier.IsCSharpKeyword()) return;

                        var diagnostic = Diagnostic.Create(Rule, token.GetLocation());
                        context.ReportDiagnostic(diagnostic);
                    }
                }
            }
        }
    }
}