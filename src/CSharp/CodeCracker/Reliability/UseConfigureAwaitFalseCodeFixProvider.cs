using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Threading;
using Microsoft.CodeAnalysis.Text;
using System.Composition;

namespace CodeCracker.Reliability
{
    [ExportCodeFixProvider("CodeCrackerUseConfigureAwaitFalseCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UseConfigureAwaitFalseCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(UseConfigureAwaitFalseAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var awaitExpression = (AwaitExpressionSyntax) root.FindNode(diagnostic.Location.SourceSpan);
            var newExpression = SyntaxFactory.InvocationExpression(
                SyntaxFactory.MemberAccessExpression(
                    SyntaxKind.SimpleMemberAccessExpression,
                    awaitExpression.Expression,
                    SyntaxFactory.IdentifierName("ConfigureAwait")),
                SyntaxFactory.ArgumentList(
                    SyntaxFactory.SingletonSeparatedList(
                        SyntaxFactory.Argument(
                            SyntaxFactory.LiteralExpression(SyntaxKind.FalseLiteralExpression)))))
                .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = root.ReplaceNode(awaitExpression.Expression, newExpression);
            var newDocument = context.Document.WithSyntaxRoot(newRoot);
            
            context.RegisterFix(
                CodeAction.Create("Use ConfigureAwait(false)", newDocument),
                diagnostic);
        }
    }
}
