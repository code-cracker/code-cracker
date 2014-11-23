using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker
{
    [ExportCodeFixProvider("UseStringBuilderToConcatenationCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UseStringBuilderToConcatenationCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(UseStringBuilderToConcatenationAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var localDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();
            context.RegisterFix(CodeAction.Create("Use 'StringBuilder'", c => UseStringBuilderAsync(context.Document, localDeclaration, c)), diagnostic);
        }

        private async Task<Document> UseStringBuilderAsync(Document document, LocalDeclarationStatementSyntax localDeclaration, CancellationToken cancellationToken)
        {
            var variableDeclaration = localDeclaration.ChildNodes()
                .OfType<VariableDeclarationSyntax>()
                .FirstOrDefault();

            var variableDeclarator = variableDeclaration.ChildNodes()
                .OfType<VariableDeclaratorSyntax>()
                .FirstOrDefault();

            var initialization = variableDeclarator.ChildNodes()
                .OfType<EqualsValueClauseSyntax>()
                .FirstOrDefault();

            var appends = GetAppends(initialization.ChildNodes());

            var root = await document.GetSyntaxRootAsync();

            var newInitialization = SyntaxFactory.EqualsValueClause(
                initialization.EqualsToken, 
                SyntaxFactory.ParseExpression(NewInitializationWithStrigBuilder(appends)));

            var newRoot = root.ReplaceNode(initialization, newInitialization);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private IEnumerable<KeyValuePair<string, int>> GetAppends(IEnumerable<SyntaxNode> nodes)
        {
            BinaryExpressionSyntax addExpression = null;
            var appends = new Dictionary<string, int>();
            
            do
            {
                nodes = addExpression?.ChildNodes() != null ? addExpression.ChildNodes() : nodes;

                addExpression = nodes
                    .OfType<BinaryExpressionSyntax>()
                    .FirstOrDefault();

                InsertItensToAppends(addExpression != null ? addExpression.ChildNodes() : nodes, appends);

            } while (addExpression != null);

            return appends.OrderBy(c => c.Value);
        }

        private void InsertItensToAppends(IEnumerable<SyntaxNode> nodes, Dictionary<string, int> appends)
        {
            foreach (var node in nodes.AsEnumerable())
            {
                var literal = node as LiteralExpressionSyntax;
                var spanStart = 0;
                string append = null;

                if (literal != null)
                {
                    append = literal.Token.Text;
                    spanStart = literal.Token.SpanStart;
                }
                else
                {
                    var variable = node as IdentifierNameSyntax;
                    if (variable != null)
                    {
                        append = variable.Identifier.Value.ToString();
                        spanStart = variable.Identifier.SpanStart;
                    }
                }

                if (!string.IsNullOrEmpty(append) && !appends.ContainsKey(append))
                    appends.Add(append, spanStart);
            }
        }

        private string NewInitializationWithStrigBuilder(IEnumerable<KeyValuePair<string, int>> itensAppend)
        {
            var sintax = new StringBuilder("new StringBuilder()");
            foreach (var item in itensAppend)
                sintax.Append(string.Format(".Append({0})", item.Key));
            sintax.Append(".ToString()");
            return sintax.ToString();
        }
    }
}
