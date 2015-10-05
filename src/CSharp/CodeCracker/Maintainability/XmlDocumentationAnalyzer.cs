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
    public sealed class XmlDocumentationAnalyzer : DiagnosticAnalyzer
    {
        internal static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.XmlDocumentationAnalyzer_Title), Resources.ResourceManager, typeof(Resources));

        internal static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId.XmlDocumentation.ToDiagnosticId(),
            Title,
            Title,
            SupportedCategories.Maintainability,
            DiagnosticSeverity.Warning,
            isEnabledByDefault: true,
            helpLinkUri: HelpLink.ForDiagnostic(DiagnosticId.XmlDocumentation));

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context) => context.RegisterSyntaxNodeAction(Analyzer, SyntaxKind.SingleLineDocumentationCommentTrivia);

        private static void Analyzer(SyntaxNodeAnalysisContext context)
        {
            var documentationNode = (DocumentationCommentTriviaSyntax)context.Node;
            var method = GetMethodFromXmlDocumentation(documentationNode);
            if (method == null) return;
            var methodParameters = method.ParameterList.Parameters;
            var xElementsWitAttrs = documentationNode.Content.OfType<XmlElementSyntax>()
                                    .Where(xEle => xEle.StartTag?.Name?.LocalName.ValueText == "param")
                                    .SelectMany(xEle => xEle.StartTag.Attributes, (xEle, attr) => attr as XmlNameAttributeSyntax)
                                    .Where(attr => attr != null);

            var keys = methodParameters.Select(parameter => parameter.Identifier.ValueText)
                .Union(xElementsWitAttrs.Select(x => x.Identifier?.Identifier.ValueText))
                .ToImmutableHashSet();

            var parameterWithDocParameter = (from key in keys
                                            where key != null
                                            let Parameter = methodParameters.FirstOrDefault(p => p.Identifier.ValueText == key)
                                            let DocParameter = xElementsWitAttrs.FirstOrDefault(p => p.Identifier?.Identifier.ValueText == key)
                                            select new { Parameter, DocParameter });

            if (parameterWithDocParameter.Any(p => p.Parameter == null))
            {
                var properties = new Dictionary<string, string> {["kind"] = "nonexistentParam" }.ToImmutableDictionary();
                var diagnostic = Diagnostic.Create(Rule, documentationNode.GetLocation(), properties);
                context.ReportDiagnostic(diagnostic);
            }

            if (parameterWithDocParameter.Any(p => p.DocParameter == null))
            {
                var properties = new Dictionary<string, string> { ["kind"] = "missingDoc" }.ToImmutableDictionary();
                var diagnostic = Diagnostic.Create(Rule, documentationNode.GetLocation(), properties);
                context.ReportDiagnostic(diagnostic);
            }
        }

        private static MethodDeclarationSyntax GetMethodFromXmlDocumentation(DocumentationCommentTriviaSyntax doc)
        {
            var tokenParent = doc.ParentTrivia.Token.Parent;
            var method = tokenParent as MethodDeclarationSyntax;
            if (method == null)
            {
                var attributeList = tokenParent as AttributeListSyntax;
                if (attributeList == null) return null;
                method = attributeList.Parent as MethodDeclarationSyntax;
            }
            return method;
        }
    }
}