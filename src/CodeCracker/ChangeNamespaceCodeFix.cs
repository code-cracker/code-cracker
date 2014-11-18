using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerChangeNamespaceCodeFix", LanguageNames.CSharp), Shared]
    public class ChangeNamespaceCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(ChangeNamespaceAnalyzer.DiagnosticId);
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
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().First();
            context.RegisterFix(CodeAction.Create("Change Namespace", c => ChangeNamespaceAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> ChangeNamespaceAsync(Document document, NamespaceDeclarationSyntax namespaceStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var newNamespaceName = ChangeNamespaceAnalyzer.ReturnFileNameSpace(semanticModel);

            var newNamespaceStatement = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(newNamespaceName))
                            .WithMembers(namespaceStatement.Members)
                            .WithLeadingTrivia(namespaceStatement.GetLeadingTrivia())
                            .WithTrailingTrivia(namespaceStatement.GetTrailingTrivia())
                            .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(namespaceStatement, newNamespaceStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}