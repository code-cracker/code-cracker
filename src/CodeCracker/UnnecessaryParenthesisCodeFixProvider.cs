using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Threading;
using System.Composition;

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerUnnecessaryParenthesisCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UnnecessaryParenthesisCodeFixProvider : CodeFixProvider
    {
        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ArgumentListSyntax>().First();

            root = root.RemoveNode(declaration, SyntaxRemoveOptions.KeepTrailingTrivia);

            var newDocument = context.Document.WithSyntaxRoot(root);
            
            context.RegisterFix(CodeAction.Create("Remove unnecessary parenthesis", newDocument), diagnostic);
        }

        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(UnnecessaryParenthesisAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
