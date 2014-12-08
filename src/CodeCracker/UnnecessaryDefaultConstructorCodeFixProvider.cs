using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerUnnecessaryDefaultConstructorCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UnnecessaryDefaultConstructorCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(UnnecessaryDefaultConstructorAnalyzer.DiagnosticId);
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();

            root = root.RemoveNode(declaration, SyntaxRemoveOptions.KeepNoTrivia);

            var newDocument = context.Document.WithSyntaxRoot(root);

            context.RegisterFix(CodeAction.Create("Remove unnecessary default constructor", newDocument), diagnostic);
        }
    }
}