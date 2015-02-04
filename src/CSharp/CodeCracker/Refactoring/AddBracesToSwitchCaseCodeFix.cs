using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeCracker.Refactoring
{
    [ExportCodeFixProvider("AddBracesToSwitchCaseCodeFixCodeFixProvider", LanguageNames.CSharp), Shared]
    public class AddBracesToSwitchCaseCodeFix : CodeFixProvider
    {
        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(AddBracesToSwitchCaseAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override Task ComputeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterFix(CodeAction.Create("Add braces to each case", ct => AddBracesAsync(context, ct)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> AddBracesAsync(CodeFixContext context, CancellationToken cancellationToken)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var @switch = (SwitchStatementSyntax) root.FindNode(diagnostic.Location.SourceSpan);
            var sections = new List<SwitchSectionSyntax>();
            foreach (var section in @switch.Sections)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!AddBracesToSwitchCaseAnalyzer.HasBraces(section))
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
            var newDocument = context.Document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static SwitchSectionSyntax AddBraces(SwitchSectionSyntax section)
        {
            StatementSyntax blockStatement = SyntaxFactory.Block(section.Statements).WithoutTrailingTrivia();
            return section.Update(section.Labels, SyntaxFactory.SingletonList(blockStatement));
        }
    }
}
