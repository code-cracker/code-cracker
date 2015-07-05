using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using CodeCracker.Properties;

namespace CodeCracker.CSharp.Maintainability
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class XmlDocumentationAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.XmlDocumentationAnalyzer_Title), Resources.ResourceManager, typeof(Resources));

        internal static DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.XmlDocumentation.ToDiagnosticId(),
            Title,
            Title,
            SupportedCategories.Maintainability,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.XmlDocumentation));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.SingleLineDocumentationCommentTrivia);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var documentationNode = (DocumentationCommentTriviaSyntax)context.Node;
            var method = documentationNode.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            var methodParameters = method.ParameterList.Parameters;
            var xElementsWitAttrs = documentationNode.Content.OfType<XmlElementSyntax>()
                                    .Where(xEle => xEle.StartTag.Name.LocalName.ValueText == "param")
                                    .SelectMany(xEle => xEle.StartTag.Attributes, (xEle, attr) => (XmlNameAttributeSyntax)attr);

            var keys = methodParameters.Select(parameter => parameter.Identifier.ValueText)
                .Union(xElementsWitAttrs.Select(x => x.Identifier.Identifier.ValueText))
                .ToImmutableHashSet();

            var paramterWithDocParameter = (from key in keys
                                            let Parameter = methodParameters.FirstOrDefault(p => p.Identifier.ValueText == key)
                                            let DocParameter = xElementsWitAttrs.FirstOrDefault(p => p.Identifier.Identifier.ValueText == key)
                                            select new { Parameter, DocParameter });

            if (paramterWithDocParameter.Any(p => p.Parameter == null))
            {
                var properties = new Dictionary<string, string> {["kind"] = "nonexistentParam" }.ToImmutableDictionary();
                var diagnostic = Diagnostic.Create(Rule, documentationNode.GetLocation(), properties);
                context.ReportDiagnostic(diagnostic);
            }

            if (paramterWithDocParameter.Any(p => p.DocParameter == null))
            {
                var properties = new Dictionary<string, string> { ["kind"] = "missingDoc" }.ToImmutableDictionary();
                var diagnostic = Diagnostic.Create(Rule, documentationNode.GetLocation(), properties);
                context.ReportDiagnostic(diagnostic);
            }
        }
    }
}
