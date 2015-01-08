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

namespace CodeCracker.Usage
{
    [ExportCodeFixProvider("CodeCrackerUnusedParametersCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UnusedParametersCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(UnusedParametersAnalyzer.DiagnosticId);
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
            var parameter = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ParameterSyntax>().First();
            context.RegisterFix(CodeAction.Create("Remove unused parameter: '\{parameter.Identifier.ValueText}'", c => RemoveParameter(context.Document, parameter, c)), diagnostic);
        }

        private async Task<Document> RemoveParameter(Document document, ParameterSyntax parameter, CancellationToken cancellationToken)
        {
            var parameterList = (ParameterListSyntax)parameter.Parent;
            var newParameterList = parameterList.WithParameters(parameterList.Parameters.Remove(parameter));
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(parameterList, newParameterList);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}