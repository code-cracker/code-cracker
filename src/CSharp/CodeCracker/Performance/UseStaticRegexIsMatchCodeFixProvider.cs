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

namespace CodeCracker.CSharp.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseStaticRegexIsMatchCodeFixProvider)), Shared]
    public class UseStaticRegexIsMatchCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.UseStaticRegexIsMatch.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Use static Regex.IsMatch", c => UseStaticRegexIsMatchAsync(context.Document, diagnostic, c)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Use static and compiled Regex.IsMatch", c => UseCompiledAndStaticRegexAsync(context.Document, diagnostic, c)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Use Compiled Regex", c => UseCompiledRegexAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> UseStaticRegexIsMatchAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken) =>
            await MakeRegexStaticAsync(document, diagnostic, cancellationToken, makeCompiled: false).ConfigureAwait(false);

        private async static Task<Document> UseCompiledAndStaticRegexAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken) =>
            await MakeRegexStaticAsync(document, diagnostic, cancellationToken, makeCompiled: true).ConfigureAwait(false);

        private async static Task<Document> MakeRegexStaticAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken, bool makeCompiled)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var invocationDeclaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var originalDeclaration = await GetObjectCreationAsync(document, invocationDeclaration, cancellationToken).ConfigureAwait(false);

            var newArgumentList = originalDeclaration.ArgumentList.Arguments.Insert(0, invocationDeclaration.ArgumentList.Arguments.FirstOrDefault());
            if (makeCompiled)
                newArgumentList = newArgumentList.Insert(2, SyntaxFactory.Argument(SyntaxFactory.IdentifierName("RegexOptions.Compiled")));

            var memberExpression = (MemberAccessExpressionSyntax)invocationDeclaration.Expression;
            var isMatchExpression = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Regex.IsMatch"),
                SyntaxFactory.ArgumentList(newArgumentList))
                .WithLeadingTrivia(memberExpression.GetLeadingTrivia())
                .WithTrailingTrivia(memberExpression.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(invocationDeclaration, isMatchExpression);
            var newDeclaratorSyntax = newRoot.FindToken(originalDeclaration.SpanStart).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();
            newRoot = newRoot.RemoveNode(newDeclaratorSyntax, SyntaxRemoveOptions.KeepNoTrivia);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private async static Task<Document> UseCompiledRegexAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var invocationDeclaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var originalDeclaration = await GetObjectCreationAsync(document, invocationDeclaration, cancellationToken).ConfigureAwait(false);
            var newArgumentList = originalDeclaration.ArgumentList.AddArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("RegexOptions.Compiled")));
            var newDeclaration = originalDeclaration.WithArgumentList(newArgumentList);
            var newRoot = root.ReplaceNode(originalDeclaration, newDeclaration);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static async Task<ObjectCreationExpressionSyntax> GetObjectCreationAsync(Document document, InvocationExpressionSyntax invocationDeclaration, CancellationToken cancellationToken)
        {
            var memberExpression = (MemberAccessExpressionSyntax)invocationDeclaration.Expression;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var variableSymbol = semanticModel.GetSymbolInfo(((IdentifierNameSyntax)memberExpression.Expression).Identifier.Parent, cancellationToken).Symbol;
            var declaratorSyntax = ((VariableDeclaratorSyntax)variableSymbol.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax());
            var originalDeclaration = declaratorSyntax.Initializer.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();
            return originalDeclaration;
        }
    }
}