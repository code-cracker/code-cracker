using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using static Microsoft.CodeAnalysis.CSharp.SyntaxKind;

namespace CodeCracker.CSharp.Maintainability
{
    [ExportCodeFixProvider(LanguageNames.CSharp, nameof(XmlDocumentationCodeFixProvider)), Shared]
    public sealed class XmlDocumentationCreateMissingParametersCodeFixProvider : XmlDocumentationCodeFixProvider
    {
        private const string WHITESPACE = @" ";

        public override SyntaxNode FixParameters(MethodDeclarationSyntax method, SyntaxNode root)
        {
            var documentationNode = method.GetLeadingTrivia().Select(x => x.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().First();
            var newDocumentationNode = documentationNode;
            var methodParameterWithDocParameter = GetMethodParametersWithDocParameters(method, documentationNode);

            var newFormation = newDocumentationNode.Content.OfType<XmlElementSyntax>();
            
            var nodesToAdd = methodParameterWithDocParameter.Where(p => p.Item2 == null)
                                .Select(x => CreateParamenterXmlDocumentation(x.Item1.Identifier.ValueText, method.Identifier.ValueText))
                                .Union(newFormation)
                                .OrderByDescending(xEle => xEle.StartTag.Name.LocalName.ValueText == "summary")
                                .ThenByDescending(xEle => xEle.StartTag.Name.LocalName.ValueText == "param")
                                .SelectMany(EnvolveXmlDocSyntaxWithNewLine)
                                .ToList();

            newDocumentationNode = newDocumentationNode.WithContent(SyntaxFactory.List(nodesToAdd));

            return root.ReplaceNode(documentationNode, newDocumentationNode);
        }

        private static XmlElementSyntax CreateParamenterXmlDocumentation(string paramenterName, string methodName)
        {
            var content = $"todo: describe {paramenterName} parameter on {methodName}";
            var xmlTagName = SyntaxFactory.XmlName(SyntaxFactory.Identifier(@"param "));
            var startTag = SyntaxFactory.XmlElementStartTag(xmlTagName).WithAttributes(CreateXmlAttributes(paramenterName));
            var endTag = SyntaxFactory.XmlElementEndTag(SyntaxFactory.XmlName(SyntaxFactory.Identifier(@"param")));

            var xmlElementSyntax = SyntaxFactory.XmlElement(startTag, endTag).WithContent(CreateXmlElementContent(content));

            return xmlElementSyntax;
        }

        private static XmlNodeSyntax[] EnvolveXmlDocSyntaxWithNewLine(XmlElementSyntax xmlElementSyntax)
        {
            var emptyTriviaList = SyntaxFactory.TriviaList();

            var syntaxTriviaList = SyntaxFactory.TriviaList(SyntaxFactory.DocumentationCommentExterior(@"///"));
            var xmlTextLiteral = SyntaxFactory.XmlTextNewLine(syntaxTriviaList, WHITESPACE, WHITESPACE, emptyTriviaList);
            var withTextTokens = SyntaxFactory.XmlText(SyntaxFactory.TokenList(xmlTextLiteral));

            var xmlNewTextLiteral = SyntaxFactory.XmlTextNewLine(emptyTriviaList, string.Empty, string.Empty, SyntaxFactory.TriviaList(SyntaxFactory.ElasticCarriageReturnLineFeed));
            var withNewLineTokens = SyntaxFactory.XmlText(SyntaxFactory.TokenList(xmlNewTextLiteral));

            return new XmlNodeSyntax[] { withTextTokens, xmlElementSyntax, withNewLineTokens };
        }

        private static SyntaxList<XmlNodeSyntax> CreateXmlElementContent(string content)
        {
            var triviaList = SyntaxFactory.TriviaList();
            var xmlTextLiteral = SyntaxFactory.XmlTextLiteral(triviaList, content, content, triviaList);
            return SyntaxFactory.SingletonList<XmlNodeSyntax>(SyntaxFactory.XmlText(SyntaxFactory.TokenList(xmlTextLiteral)));
        }

        private static SyntaxList<XmlAttributeSyntax> CreateXmlAttributes(string paramenterName)
        {
            var xmlNameSyntax = SyntaxFactory.XmlName(SyntaxFactory.Identifier("name"));
            var quoteToken = SyntaxFactory.Token(DoubleQuoteToken);
            var identifierNameSyntax = SyntaxFactory.IdentifierName(paramenterName.Trim());
            var equalsToken = SyntaxFactory.Token(EqualsToken);
            var xmlNameAttributeSyntax = SyntaxFactory.XmlNameAttribute(xmlNameSyntax, equalsToken, quoteToken, identifierNameSyntax, quoteToken);
            return SyntaxFactory.SingletonList<XmlAttributeSyntax>(xmlNameAttributeSyntax);
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
