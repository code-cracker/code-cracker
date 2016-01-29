using System.Threading.Tasks;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis;
using System.Linq;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Refactoring
{
    public sealed class AddPropertyToConstructorCodeFixProviderAll : FixAllProvider
    {
        private static readonly SyntaxAnnotation addPropertyToConstructor = new SyntaxAnnotation(nameof(AddPropertyToConstructorCodeFixProviderAll));
        private AddPropertyToConstructorCodeFixProviderAll() { }
        public static readonly AddPropertyToConstructorCodeFixProviderAll Instance = new AddPropertyToConstructorCodeFixProviderAll();

        public override Task<CodeAction> GetFixAsync(FixAllContext fixAllContext)
        {
            if (fixAllContext.Scope == FixAllScope.Document)
                return Task.FromResult(CodeAction.Create(AddPropertyToConstructorAnalyzer.MessageFormat,
                    async ct => fixAllContext.Document.WithSyntaxRoot(await GetFixedDocumentAsync(fixAllContext, fixAllContext.Document))));
            return null;
        }


        private async static Task<SyntaxNode> GetFixedDocumentAsync(FixAllContext fixAllContext, Document document)
        {
            var diagnostics = await fixAllContext.GetDocumentDiagnosticsAsync(document).ConfigureAwait(false);
            var root = await document.GetSyntaxRootAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            var nodes = diagnostics.Select(d => root.FindNode(d.Location.SourceSpan)).Where(n => !n.IsMissing);
            var newRoot = root.ReplaceNodes(nodes, (original, rewritten) => original.WithAdditionalAnnotations(addPropertyToConstructor));
            var semanticModel = await document.GetSemanticModelAsync(fixAllContext.CancellationToken).ConfigureAwait(false);
            while (true)
            {
                var annotatedNodes = newRoot.GetAnnotatedNodes(addPropertyToConstructor);
                var node = annotatedNodes.FirstOrDefault();
                if (node == null) break;
                var currentProperty = (PropertyDeclarationSyntax)node;
                var currentClass = (ClassDeclarationSyntax)currentProperty.Parent;
                newRoot = AddPropertyToConstructorFixProvider.AddPropertyToConstructor(newRoot, currentClass, currentProperty);
                node = newRoot.GetAnnotatedNodes(addPropertyToConstructor).First();
                newRoot = newRoot.ReplaceNode(node, node.WithoutAnnotations(addPropertyToConstructor));
            }
            return newRoot;
        }
    }
}
