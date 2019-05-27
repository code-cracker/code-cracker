using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Maintainability
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(XmlDocumentationCodeFixProvider)), Shared]
    public sealed class XmlDocumentationMissingInCSharpCodeFixProvider : XmlDocumentationCodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.XmlDocumentation_MissingInCSharp.ToDiagnosticId());

        public override SyntaxNode FixParameters(MethodDeclarationSyntax method, SyntaxNode root)
        {
            var documentationNode = method.GetLeadingTrivia().Select(x => x.GetStructure()).OfType<DocumentationCommentTriviaSyntax>().First();

            var allNodesToRemove = GetAllNodesToRemove(GetMethodParametersWithDocParameters(method, documentationNode), documentationNode);

            var newDocumentationNode = documentationNode.RemoveNodes(allNodesToRemove, SyntaxRemoveOptions.KeepNoTrivia);

            return root.ReplaceNode(documentationNode, newDocumentationNode);
        }

        private static IEnumerable<SyntaxNode> GetAllNodesToRemove(IEnumerable<Tuple<ParameterSyntax, Tuple<XmlElementSyntax, XmlNameAttributeSyntax>>> paramterWithDocParameter, DocumentationCommentTriviaSyntax documentationNode)
        {
            var nodesToRemove = paramterWithDocParameter.Where(p => p.Item1 == null).Select(x => x.Item2.Item1).ToList();

            var xmlTextNodesToRemove = documentationNode.Content.OfType<XmlTextSyntax>()
                .Join(nodesToRemove, textNode => textNode.FullSpan.End, tagNode => tagNode.FullSpan.Start, (textNode, tagNode) => textNode);

            var allNodesToRemove = nodesToRemove.Cast<SyntaxNode>().Union(xmlTextNodesToRemove);
            return allNodesToRemove;
        }

        public override sealed Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(Resources.XmlDocumentationRemoveNonExistentParametersCodeFixProvider_Title, c => FixParametersAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }
    }
}