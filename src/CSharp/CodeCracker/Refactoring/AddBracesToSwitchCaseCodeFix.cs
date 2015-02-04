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

        public override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var caseLabel = (CaseSwitchLabelSyntax) root.FindNode(diagnostic.Location.SourceSpan);
            var section = caseLabel.Parent as SwitchSectionSyntax;
            if (section == null)
                return;
            StatementSyntax blockStatement = SyntaxFactory.Block(section.Statements).WithAdditionalAnnotations(Formatter.Annotation);
            var newSection = section.Update(section.Labels, SyntaxFactory.SingletonList(blockStatement));
            var newRoot = root.ReplaceNode(section, newSection);
            var newDocument = context.Document.WithSyntaxRoot(newRoot);
            context.RegisterFix(CodeAction.Create("Add braces", newDocument), diagnostic);
        }
    }
}
