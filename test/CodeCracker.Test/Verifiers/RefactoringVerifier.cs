using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeRefactorings;
using Microsoft.CodeAnalysis.Text;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace TestHelper
{
    public abstract class RefactoringVerifier
    {
        protected const char StartMarker = '├';
        protected const char EndMarker = '┤';
        protected virtual CodeRefactoringProvider GetCSharpCodeRefactoringProvider()
        {
            return null;
        }

        public Task VerifyCSharpRefactoringAsync(string source, string expectedSourceAfter,
            string codeActionId)
        {
            string cleanSource;
            TextSpan span;
            ExtractSpanFromSource(source, out cleanSource, out span);

            return VerifyCSharpRefactoringAsync(cleanSource, span, expectedSourceAfter, codeActionId);
        }

        private static void ExtractSpanFromSource(string source, out string cleanSource, out TextSpan span)
        {
            var startTokenPosition = source.IndexOf(StartMarker);
            Assert.InRange(startTokenPosition, 0, source.Length-1);

            var endTokenPosition = source.IndexOf(EndMarker);
            Assert.InRange(endTokenPosition, 0, source.Length-1);

            Assert.True(endTokenPosition > startTokenPosition);

            cleanSource=source.Remove(startTokenPosition, 1).Remove(endTokenPosition-1, 1);
            span=new TextSpan(startTokenPosition, endTokenPosition-startTokenPosition-1);
        }

        public async Task VerifyCSharpRefactoringAsync(string source, TextSpan span, string expectedSourceAfter,
            string codeActionId)
        {
            CustomWorkspace workspace;
            var oldProject = TestHelpers.CreateProject(new[] { source }, out workspace);

            var allCodeActions = await GetCodeActions(oldProject, span);
            Assert.NotEmpty(allCodeActions);

            var codeActions = allCodeActions.Where(a => a.Id == codeActionId).ToImmutableList();
            Assert.Equal(1, codeActions.Count);
            
            await ApplyCodeAction(workspace, codeActions.Single());

            var newDocument = workspace.CurrentSolution.Projects.First().Documents.First();
            var sourceAfter = await TestHelpers.GetStringFromDocumentAsync(newDocument);
            Assert.Equal(expectedSourceAfter, sourceAfter);
        }

        public Task VerifyNoCSharpRefactoringAsync(string source)
        {
            string cleanSource;
            TextSpan span;
            ExtractSpanFromSource(source, out cleanSource, out span);
            return VerifyNoCSharpRefactoringAsync(cleanSource, span);
        }

        public async Task VerifyNoCSharpRefactoringAsync(string source, TextSpan span)
        {
            CustomWorkspace workspace;
            var oldProject = TestHelpers.CreateProject(new[] { source }, out workspace);

            var codeActions = await GetCodeActions(oldProject, span);
            Assert.Empty(codeActions);
        }

        private async Task<ImmutableList<CodeAction>> GetCodeActions(Project project, TextSpan span)
        {
            var document = project.Documents.First();
            var codeActions = ImmutableList.Create<CodeAction>();
            var context = new CodeRefactoringContext(document, span,
                a => codeActions=codeActions.Add(a), CancellationToken.None);

            var provider = GetCSharpCodeRefactoringProvider();
            Assert.NotNull(provider);

            await provider.ComputeRefactoringsAsync(context).ConfigureAwait(false);

            return codeActions;
        }

        private static async Task ApplyCodeAction(CustomWorkspace workspace, CodeAction codeAction)
        {
            var operations = await codeAction.GetOperationsAsync(CancellationToken.None);
            foreach (var operation in operations)
            {
                operation.Apply(workspace, CancellationToken.None);
            }
        }
    }
}
