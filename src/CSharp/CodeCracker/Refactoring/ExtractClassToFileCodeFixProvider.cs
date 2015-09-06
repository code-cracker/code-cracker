using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Threading;
using Microsoft.CodeAnalysis.Text;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtractClassToFileCodeFixProvider)), Shared]
    public class ExtractClassToFileCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.ExtractClassToFile.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Extract class to new file", ct => ExtractClassAsync(context.Document, diagnostic, ct), nameof(ExtractClassToFileCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Solution> ExtractClassAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var classStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeSymbol = semanticModel.GetDeclaredSymbol(classStatement, cancellationToken);

            var newRoot = root.RemoveNode(classStatement,SyntaxRemoveOptions.KeepNoTrivia);
            document = document.WithSyntaxRoot(newRoot);

            var usings = root.DescendantNodesAndSelf().Where(u => u is UsingDirectiveSyntax).Select(u => (UsingDirectiveSyntax)u);
            var nameSpaceBlock = SyntaxFactory.NamespaceDeclaration(SyntaxFactory.IdentifierName(typeSymbol.ContainingNamespace.Name));
            nameSpaceBlock = nameSpaceBlock.WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(classStatement));

            var newFileClass = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List<UsingDirectiveSyntax>((usings)))
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>(nameSpaceBlock))
                .WithoutLeadingTrivia()
                .NormalizeWhitespace();
            var filename = $"{classStatement.Identifier.Text}.cs";
            var newDocument = document.Project.AddDocument(filename, SourceText.From(newFileClass.ToFullString()), document.Folders);
            return newDocument.Project.Solution;
        }

        
    }
}