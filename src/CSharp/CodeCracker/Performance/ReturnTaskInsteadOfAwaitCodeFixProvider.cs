using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Composition;
using CodeCracker.Properties;

namespace CodeCracker.CSharp.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReturnTaskInsteadOfAwaitCodeFixProvider)), Shared]
    public class ReturnTaskInsteadOfAwaitCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
                   ImmutableArray.Create(DiagnosticId.ReturnTaskInsteadOfAwait.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(Resources.ReturnTaskInsteadOfAwaitCodeFixProvider_Title, c => ReturnTaskDirectly(context.Document, diagnostic, c), nameof(RemoveWhereWhenItIsPossibleCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> ReturnTaskDirectly(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var methodDecl = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            var newMethod = RemoveAsyncModifier(methodDecl);

            var awaits = (from child in newMethod.Body.DescendantNodes(_ => true)
                          where child.IsKind(SyntaxKind.AwaitExpression)
                          select new
                          {
                              Statement = child.FirstAncestorOrSelfThatIsAStatement(),
                              Await = child as AwaitExpressionSyntax
                          }).ToDictionary(i => i.Statement, i => i.Await);

            newMethod = newMethod.ReplaceNodes(awaits.Keys, (statement, _) =>
                SyntaxFactory.ReturnStatement(awaits[statement].Expression));

            if (newMethod.ReturnType.ToString() == "void")
            {
                newMethod = newMethod.WithReturnType(SyntaxFactory.ParseTypeName(nameof(Task))
                    .WithLeadingTrivia(newMethod.ReturnType.GetLeadingTrivia())
                    .WithTrailingTrivia(newMethod.ReturnType.GetTrailingTrivia()));
            }

            var newRoot = root.ReplaceNode(methodDecl, newMethod)
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        /// <summary>
        /// Removes the 'async' modifier without removing the trivia.
        /// </summary>
        /// <param name="methodDecl"></param>
        /// <returns></returns>
        private static MethodDeclarationSyntax RemoveAsyncModifier(MethodDeclarationSyntax methodDecl)
        {
            var modifiers = methodDecl.Modifiers;
            var asyncIndex = modifiers.IndexOf(SyntaxKind.AsyncKeyword);
            var async = modifiers[asyncIndex];

            if (modifiers.Count == 1)
            {
                var newMethod = methodDecl.WithModifiers(new SyntaxTokenList());
                var returnTypeTrivia = newMethod.ReturnType.GetLeadingTrivia()
                    .AddRange(async.LeadingTrivia)
                    .AddRange(async.TrailingTrivia)
                    .AddRange(newMethod.Identifier.LeadingTrivia);
                newMethod = newMethod.WithReturnType(newMethod.ReturnType.WithLeadingTrivia(returnTypeTrivia));
                return newMethod;
            }
            else
            {
                if (asyncIndex == 0)
                {
                    var nextModifier = modifiers[1];
                    var newNextModifier = nextModifier.WithLeadingTrivia(
                        nextModifier.LeadingTrivia
                        .AddRange(async.LeadingTrivia)
                        .AddRange(async.TrailingTrivia)
                        .AddRange(nextModifier.LeadingTrivia));

                    modifiers = modifiers.Replace(nextModifier, newNextModifier);
                }
                else
                {
                    var nextModifier = modifiers[asyncIndex - 1];
                    var newModifier = nextModifier.WithTrailingTrivia(
                        nextModifier.TrailingTrivia
                        .AddRange(async.LeadingTrivia)
                        .AddRange(async.TrailingTrivia)
                        .AddRange(nextModifier.TrailingTrivia));

                    modifiers = modifiers.Replace(nextModifier, newModifier);

                }
                modifiers = modifiers.RemoveAt(asyncIndex);
                return methodDecl.WithModifiers(modifiers);
            }
        }
    }
}