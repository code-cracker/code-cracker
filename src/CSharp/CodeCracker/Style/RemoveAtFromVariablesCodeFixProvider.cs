using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Style
{
    public class RemoveAtFromVariablesCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords.ToDiagnosticId(), DiagnosticId.RemoveAtFromVariablesThatAreNotKeywords.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Remove @ from Variables that are not keywords", c => RemoveAtFromVariableNamesThatAreNotKeywords(context.Document, diagnostic, c), nameof(RemoveAtFromVariablesCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> RemoveAtFromVariableNamesThatAreNotKeywords(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var localDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();
            var variableDeclaration = localDeclaration.Declaration;

            var variable = variableDeclaration.Variables.First();
            var leading = variable.Identifier.LeadingTrivia;
            var trailing = variable.Identifier.TrailingTrivia;

            if (variable.Identifier.Text.StartsWith(@"@") && variable.Identifier.Text.IsCSharpKeyword() == false)
            {
                var newVariable = variable.WithIdentifier(SyntaxFactory.Identifier(leading, variable.Identifier.ValueText, trailing));

                var newRoot = root.ReplaceNode(variable, newVariable);
                var newDocument = document.WithSyntaxRoot(newRoot);
                return newDocument;
            }

            return document;
        }
    }
}
