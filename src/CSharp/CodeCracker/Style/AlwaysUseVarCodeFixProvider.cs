using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AlwaysUseVarCodeFixProvider)), Shared]
    public class AlwaysUseVarCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.AlwaysUseVar.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Use 'var'", c => UseVarAsync(context.Document, diagnostic, c), nameof(AlwaysUseVarCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> UseVarAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var localDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();
            var variableDeclaration = localDeclaration.ChildNodes()
                .OfType<VariableDeclarationSyntax>()
                .FirstOrDefault();
            var @var = SyntaxFactory.IdentifierName("var")
                .WithLeadingTrivia(variableDeclaration.Type.GetLeadingTrivia())
                .WithTrailingTrivia(variableDeclaration.Type.GetTrailingTrivia());
            var newRoot = root.ReplaceNode(variableDeclaration.Type, @var);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}