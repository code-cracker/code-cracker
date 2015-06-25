using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CodeCracker.CSharp.Design.InconsistentAccessibility;
using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Simplification;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(InconsistentAccessibilityCodeFixProvider)), Shared]
    class MakeMethodNonAsyncCodeFixProvider : CodeFixProvider
    {
        internal const string AsyncMethodLacksAwaitCompilerWarningNumber = "CS1998";

        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(AsyncMethodLacksAwaitCompilerWarningNumber);

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create(
                    Resources.MakeMethodNonAsyncCodeFixProvider_Title,
                    ct => MakeMethodNonAsyncAsync(context.Document, diagnostic, ct)),
                diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> MakeMethodNonAsyncAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            
            var methodDeclaration = node.FirstAncestorOrSelfOfType<MethodDeclarationSyntax>();
            var rewriter = new ReturnTaskFromResultRewriter();
            var asyncKeyword = methodDeclaration.Modifiers.First(t => t.Kind() == SyntaxKind.AsyncKeyword);
            var newMethodDeclaration =
                methodDeclaration
                    .WithModifiers(methodDeclaration.Modifiers.Remove(asyncKeyword))
                    .WithBody((BlockSyntax) rewriter.VisitBlock(methodDeclaration.Body));
            var newRoot = root.ReplaceNode(methodDeclaration, newMethodDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }

        private class ReturnTaskFromResultRewriter : CSharpSyntaxRewriter
        {
            public override SyntaxNode VisitReturnStatement(ReturnStatementSyntax node)
            {

                var newNode = node.WithExpression(
                    SyntaxFactory.InvocationExpression(
                        SyntaxFactory.MemberAccessExpression(
                            SyntaxKind.SimpleMemberAccessExpression,
                            SyntaxFactory.ParseName("System.Threading.Tasks.Task")
                                .WithAdditionalAnnotations(Simplifier.Annotation),
                            SyntaxFactory.IdentifierName("FromResult")),
                        SyntaxFactory.ArgumentList().AddArguments(SyntaxFactory.Argument(node.Expression))));
                return base.VisitReturnStatement(newNode);
            }
        }
    }
}
