using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Maintainability
{
    public abstract class XmlDocumentationCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.XmlDocumentation.ToDiagnosticId());

        public abstract SyntaxNode FixParameters(MethodDeclarationSyntax method, SyntaxNode root);

        protected async Task<Document> FixParametersAsync(Document document, Diagnostic diagnostic, CancellationToken c)
        {
            var root = await document.GetSyntaxRootAsync(c).ConfigureAwait(false);
            var documentationNode = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            var newRoot = FixParameters(documentationNode, root);
            var newDocument = document.WithSyntaxRoot(newRoot);

            return newDocument;
        }

        protected static IEnumerable<Tuple<ParameterSyntax, Tuple<XmlElementSyntax, XmlNameAttributeSyntax>>> GetMethodParametersWithDocParameters(MethodDeclarationSyntax method, DocumentationCommentTriviaSyntax documentationNode)
        {
            var methodParameters = method.ParameterList.Parameters;

            var xElementsWitAttrs = documentationNode.Content.OfType<XmlElementSyntax>()
                                    .Where(xEle => xEle.StartTag.Name.LocalName.ValueText == "param")
                                    .SelectMany(xEle => xEle.StartTag.Attributes, (xEle, attr) => new Tuple<XmlElementSyntax, XmlNameAttributeSyntax>(xEle, (XmlNameAttributeSyntax)attr));

            var keys = methodParameters.Select(parameter => parameter.Identifier.ValueText)
                .Union(xElementsWitAttrs.Select(x => x.Item2.Identifier.Identifier.ValueText))
                .ToImmutableHashSet();

            return (from key in keys
                    let Parameter = methodParameters.FirstOrDefault(p => p.Identifier.ValueText == key)
                    let DocParameter = xElementsWitAttrs.FirstOrDefault(p => p.Item2.Identifier.Identifier.ValueText == key)
                    select new Tuple<ParameterSyntax, Tuple<XmlElementSyntax, XmlNameAttributeSyntax>>(Parameter, DocParameter));
        }
    }
}
