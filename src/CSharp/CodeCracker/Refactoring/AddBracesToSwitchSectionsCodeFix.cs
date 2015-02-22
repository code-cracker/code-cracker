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

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider("AddBracesToSwitchCaseCodeFixCodeFixProvider", LanguageNames.CSharp), Shared]
    public class AddBracesToSwitchSectionsCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.AddBracesToSwitchSections.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var @switch = (SwitchStatementSyntax) root.FindNode(diagnostic.Location.SourceSpan);
            var newDoc = AddBracesAsync(context.Document, root, @switch);
            context.RegisterFix(CodeAction.Create("Add braces to each switch section", newDoc), diagnostic);
        }

        private static Document AddBracesAsync(Document document, SyntaxNode root, SwitchStatementSyntax @switch)
        {
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
