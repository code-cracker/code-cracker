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

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AddBracesToSwitchSectionsCodeFixProvider)), Shared]
    public class AddBracesToSwitchSectionsCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Add braces to each switch section", ct => AddBracesAsync(context.Document, diagnostic, ct)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> AddBracesAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var @switch = (SwitchStatementSyntax)root.FindNode(diagnostic.Location.SourceSpan);
            var sections = new List<SwitchSectionSyntax>();
            foreach (var section in @switch.Sections)
            {
                if (!AddBracesToSwitchSectionsAnalyzer.HasBraces(section))
                {
                    var newSection = AddBraces(section);
                    sections.Add(newSection);
                }
                else
                {
                    sections.Add(section);
                }
            }
            var newSwitch = @switch.WithSections(SyntaxFactory.List(sections)).WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(@switch, newSwitch);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static SwitchSectionSyntax AddBraces(SwitchSectionSyntax section)
        {
            StatementSyntax blockStatement = SyntaxFactory.Block(section.Statements).WithoutTrailingTrivia();
            return section.Update(section.Labels, SyntaxFactory.SingletonList(blockStatement));
        }
    }
}