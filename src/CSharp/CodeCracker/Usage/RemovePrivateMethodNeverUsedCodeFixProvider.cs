using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using System.Composition;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using System.Threading;

namespace CodeCracker.Usage
{
    [ExportCodeFixProvider("RemovePrivateMethodNeverUsedCodeFixProvider", LanguageNames.CSharp), Shared]
    public class RemovePrivateMethodNeverUsedCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(DiagnosticId.RemovePrivateMethodNeverUsed.ToDiagnosticId());
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

            var methodNotUsed = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            context.RegisterFix(CodeAction.Create($"Remove unused private method : '{methodNotUsed.Identifier.Text}'", c => RemoveMethod(context.Document, methodNotUsed, c)), diagnostic);
        }

        private async Task<Document> RemoveMethod(Document document, MethodDeclarationSyntax methodNotUsed, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newRoot = root.RemoveNode(methodNotUsed, SyntaxRemoveOptions.KeepNoTrivia);

            return document.WithSyntaxRoot(newRoot);

        }
    }
}
