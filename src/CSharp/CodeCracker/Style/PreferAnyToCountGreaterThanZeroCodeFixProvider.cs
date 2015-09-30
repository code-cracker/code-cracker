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
using Microsoft.CodeAnalysis.Formatting;
using CodeCracker.Properties;

namespace CodeCracker.CSharp.Style
{

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PreferAnyToCountGreaterThanZeroCodeFixProvider)), Shared]
    public class PreferAnyToCountGreaterThanZeroCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.PreferAnyToCountGreaterThanZero.ToDiagnosticId());
        private static readonly SimpleNameSyntax anyName = (SimpleNameSyntax)SyntaxFactory.ParseName("Any");

        public override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(Resources.PreferAnyToCountGreaterThanZeroAnalyzer_Title, c => ConvertToAnyAsync(context.Document, diagnostic, c), nameof(PreferAnyToCountGreaterThanZeroCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private static async Task<Document> ConvertToAnyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = (CompilationUnitSyntax)await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var node = root.FindNode(diagnostic.Location.SourceSpan);
            var greaterThanExpression = (BinaryExpressionSyntax)node;
            var leftExpression = greaterThanExpression.Left;
            var memberExpression = leftExpression.DescendantNodesAndSelf().OfType<MemberAccessExpressionSyntax>().FirstOrDefault();
            var invocationExpression = leftExpression as InvocationExpressionSyntax;
            var anyExpression = invocationExpression == null ? SyntaxFactory.InvocationExpression(memberExpression.WithName(anyName)) : SyntaxFactory.InvocationExpression(memberExpression.WithName(anyName), invocationExpression.ArgumentList)
                .WithLeadingTrivia(greaterThanExpression.GetLeadingTrivia())
                .WithTrailingTrivia(greaterThanExpression.GetTrailingTrivia());
            var newRoot = root.ReplaceNode(greaterThanExpression, anyExpression);
            newRoot = AddUsingSystemLinq(root, newRoot);
            return document.WithSyntaxRoot(newRoot);
        }

        private static CompilationUnitSyntax AddUsingSystemLinq(CompilationUnitSyntax root, CompilationUnitSyntax newRoot)
        {
            var isUsingSystemLinq = root.Usings.Any(u => u.Name.GetText().ToString() == "System.Linq");
            if (!isUsingSystemLinq)
                newRoot = newRoot.AddUsings(SyntaxFactory.UsingDirective(SyntaxFactory.ParseName("System.Linq")));
            return newRoot;
        }
    }
}