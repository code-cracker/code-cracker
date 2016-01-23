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

/// <summary>
/// xpto
/// </summary>
namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ExtractClassToFileCodeFixProvider)), Shared]
    public class ExtractClassToFileCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.ExtractClassToFile.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => null;

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

            var ns = classStatement.FirstAncestorOfType<NamespaceDeclarationSyntax>();
            NamespaceDeclarationSyntax newNs = null;

            SyntaxNode namespaceRemove = classStatement;
            var canContinue = true;
            while (ns != null)
            {
                if (ns.Members.Count() == 1 )
                {
                    if (canContinue)
                    {
                        namespaceRemove = ns;
                    }
                }
                else
                {
                    canContinue = false;
                }

                if (newNs != null)
                {
                    var newNewNs = SyntaxFactory.NamespaceDeclaration(ns.Name)
                        .WithUsings(ns.Usings)
                        .WithLeadingTrivia(ns.GetLeadingTrivia())
                        .AddMembers(newNs);
                    newNs = newNewNs;
                }
                else
                {
                    newNs = SyntaxFactory.NamespaceDeclaration(ns.Name)
                        .WithUsings(ns.Usings)
                        .WithLeadingTrivia(ns.GetLeadingTrivia())
                        .AddMembers(classStatement);
                }
                ns = ns.FirstAncestorOfType<NamespaceDeclarationSyntax>();
            }

            var newRoot = root.RemoveNode(namespaceRemove, SyntaxRemoveOptions.KeepNoTrivia);
            
            document = document.WithSyntaxRoot(newRoot);

            var usings = root.ChildNodes().Where(n => n.IsKind(SyntaxKind.UsingDirective)).Cast<UsingDirectiveSyntax>();
            var newFileClass = SyntaxFactory.CompilationUnit()
                .WithUsings(SyntaxFactory.List<UsingDirectiveSyntax>((usings)))
                .WithMembers(SyntaxFactory.SingletonList<MemberDeclarationSyntax>((MemberDeclarationSyntax)newNs ?? classStatement))
                .WithoutLeadingTrivia()
                .NormalizeWhitespace()
                .WithAdditionalAnnotations(Formatter.Annotation);
            var filename = $"{classStatement.Identifier.Text}.cs";
            var newDocument = document.Project.AddDocument(filename, SourceText.From(newFileClass.ToFullString()), document.Folders);
            return newDocument.Project.Solution;
        }


    }
}

