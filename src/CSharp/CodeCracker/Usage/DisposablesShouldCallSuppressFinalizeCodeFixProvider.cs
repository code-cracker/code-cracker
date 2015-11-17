using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(DisposablesShouldCallSuppressFinalizeCodeFixProvider)), Shared]
    public class DisposablesShouldCallSuppressFinalizeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(
                CodeAction.Create("Call GC.SuppressFinalize", ct => AddSuppressFinalizeAsync(context.Document, diagnostic, ct), nameof(DisposablesShouldCallSuppressFinalizeCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> AddSuppressFinalizeAsync(
            Document document, 
            Diagnostic diagnostic, 
            CancellationToken cancellationToken
            )
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);

            var startLocation = diagnostic.Location.SourceSpan.Start;
            var token = root.FindToken(startLocation);
            var method = token.Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            var gc = IsThereUsingSystem(root)
                ? (ExpressionSyntax) SyntaxFactory.IdentifierName("GC")
                : SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    SyntaxFactory.IdentifierName("System"),
                    SyntaxFactory.IdentifierName("GC")
                    );

            return document
                .WithSyntaxRoot(root
                .ReplaceNode(method, method.AddBodyStatements(
                    SyntaxFactory.ExpressionStatement(
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberAccessExpression(
                                SyntaxKind.SimpleMemberAccessExpression,
                                gc,
                                SyntaxFactory.IdentifierName("SuppressFinalize")),
                            SyntaxFactory.ArgumentList().AddArguments(
                                SyntaxFactory.Argument(SyntaxFactory.ThisExpression())))))
                    .WithAdditionalAnnotations(Formatter.Annotation)));
        }


        public static bool IsThereUsingSystem(SyntaxNode root)
        {
            return root
                .DescendantNodesAndSelf()
                .OfType<UsingDirectiveSyntax>()
                .Any(u => u.Name.ToString() == "System");
        }
    }
}