using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using CodeCracker.Properties;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace CodeCracker.CSharp.Maintainability
{
    [ExportCodeFixProvider(LanguageNames.CSharp, nameof(XmlDocumentationCodeFixProvider)), Shared]
    public class XmlDocumentationCreateMissingParametersCodeFixProvider : XmlDocumentationCodeFixProvider
    {
        public override SyntaxNode FixParameters(MethodDeclarationSyntax method, SyntaxNode root)
        {
            var documentationNode = method.GetLeadingTrivia().Select(x => x.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().First();
            var newDocumentationNode = documentationNode;
            var methodParameterWithDocParameter = GetMethodParametersWithDocParameters(method, documentationNode);

            var xmlText = documentationNode.Content.OfType<XmlTextSyntax>().Skip(1).First();

            var nodesToAdd = methodParameterWithDocParameter.Where(p => p.Item2 == null)
                                .SelectMany(x => CreateParamenterXmlDocumentation(xmlText, x.Item1.Identifier.ValueText, method.Identifier.ValueText))
                                .ToList();
            var newFormation = newDocumentationNode.Content.OfType<XmlElementSyntax>().ToList();
            var node = newFormation.LastOrDefault(xEle => xEle.StartTag.Name.LocalName.ValueText == "param") ?? newFormation.LastOrDefault(xEle => xEle.StartTag.Name.LocalName.ValueText == "summary");
            var nodeInList = documentationNode.Content.OfType<XmlTextSyntax>().FirstOrDefault(x => x.FullSpan.Start == node.FullSpan.End);

            newDocumentationNode = newDocumentationNode.InsertNodesAfter(node, nodesToAdd.Cast<SyntaxNode>());

            return root.ReplaceNode(documentationNode, newDocumentationNode);
        }


        private static XmlNodeSyntax[] CreateParamenterXmlDocumentation(XmlTextSyntax xmlText, string paramenterName, string methodName)
        {
            var content = $"todo: describe {paramenterName} parameter on {methodName}";
            var xmlTagName = SyntaxFactory.XmlName(SyntaxFactory.Identifier(@"param "));
            var startTag = SyntaxFactory.XmlElementStartTag(xmlTagName).WithAttributes(CreateXmlAttributes(paramenterName));
            var endTag = SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName(SyntaxFactory.Identifier(@"param")));

            var xmlElementSyntax = SyntaxFactory.XmlElement(startTag, endTag).WithContent(CreateXmlElementContent(content));

            return new XmlNodeSyntax[] { xmlText, xmlElementSyntax };
        }

        private static SyntaxList<XmlNodeSyntax> CreateXmlElementContent(string content)
        {
            var triviaList = SyntaxFactory.TriviaList();
            var xmlTextLiteral = SyntaxFactory.XmlTextLiteral(triviaList, content, content, triviaList);
            return SyntaxFactory.SingletonList<XmlNodeSyntax>(SyntaxFactory.XmlText(SyntaxFactory.TokenList(xmlTextLiteral)));
        }

        private static SyntaxList<XmlAttributeSyntax> CreateXmlAttributes(string paramenterName)
        {
            var syntaxToken = SyntaxFactory.Token(DoubleQuoteToken);
            var xmlAttributeName = SyntaxFactory.XmlName(SyntaxFactory.Identifier(@"name"));

            return SyntaxFactory.SingletonList<XmlAttributeSyntax>(SyntaxFactory.XmlNameAttribute(xmlAttributeName, syntaxToken, SyntaxFactory.IdentifierName(paramenterName), syntaxToken));
        }

        public override sealed Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            if (diagnostic.Properties["kind"] == "missingDoc")
                context.RegisterCodeFix(CodeAction.Create(Resources.XmlDocumentationCreateMissingParametersCodeFixProvider_Title, c => FixParametersAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }
    }


}
