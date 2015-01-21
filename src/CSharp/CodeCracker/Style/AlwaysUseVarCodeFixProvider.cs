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

namespace CodeCracker.Style
{

    [ExportCodeFixProvider("CodeCrackerAlwaysUseVarCodeFixProvider", LanguageNames.CSharp), Shared]
    public class AlwaysUseVarCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(AlwaysUseVarAnalyzer.DiagnosticId);
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
            const string message = "Use 'var'";
            context.RegisterFix(CodeAction.Create(message, c => UseVarAsync(context.Document, localDeclaration, c)), diagnostic);
        }

        private async Task<Document> UseVarAsync(Document document, LocalDeclarationStatementSyntax localDeclaration, CancellationToken cancellationToken)
        {
            var variableDeclaration = localDeclaration.ChildNodes()
                .OfType<VariableDeclarationSyntax>()
                .FirstOrDefault();
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var @var = SyntaxFactory.IdentifierName("var")
                .WithLeadingTrivia(variableDeclaration.Type.GetLeadingTrivia())
                .WithTrailingTrivia(variableDeclaration.Type.GetTrailingTrivia());
            var newRoot = root.ReplaceNode(variableDeclaration.Type, @var);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}