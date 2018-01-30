using System.Linq;
using System.Threading;
using System.Composition;
using Microsoft.CodeAnalysis;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SortUsingsCodeFixProvider)), Shared]
    public class SortUsingsCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.SortUsings.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Sort by length", ct => SortUsingsAsync(context.Document, diagnostic, ct), nameof(SortUsingsCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> SortUsingsAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var sourceSpan = (CompilationUnitSyntax)root.FindNode(diagnostic.Location.SourceSpan);

            var collector = new UsingCollector();
            collector.Visit(root);

            var sections = new List<UsingDirectiveSyntax>();
            sections.AddRange(collector.Usings
                .OrderBy(u => u.ToString().Contains(" static "))
                .ThenBy(u => u.ToString().Length));

            var newUsings = sourceSpan.WithUsings(SyntaxFactory.List(sections)).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(sourceSpan, newUsings);
            return document.WithSyntaxRoot(newRoot);
        }
    }

    internal sealed class UsingCollector : CSharpSyntaxWalker
    {
        private readonly List<UsingDirectiveSyntax> usingCollection = new List<UsingDirectiveSyntax>();

        public IEnumerable<UsingDirectiveSyntax> Usings => usingCollection;

        public override void VisitUsingDirective(UsingDirectiveSyntax node)
        {
            this.usingCollection.Add(node);
        }
    }
}
