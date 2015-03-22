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
    [ExportCodeFixProvider("CodeCrackerUseStaticRegexIsMatchCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UseStaticRegexIsMatchCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.UseStaticRegexIsMatch.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var invocationDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            context.RegisterCodeFix(CodeAction.Create("Use static Regex.IsMatch", c => UseStaticRegexIsMatch(context.Document, invocationDeclaration, c)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Use Compiled Regex", c => UseCompiledRegex(context.Document, invocationDeclaration, c)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Use compiled and static Regex.IsMatch", c => UseCompiledAndStaticRegex(context.Document, invocationDeclaration, c)), diagnostic);
        }

        private async Task<Document> MakeRegexStatic(Document document, InvocationExpressionSyntax invocationDeclaration, CancellationToken cancellationToken, bool makeCompiled = false)
        {
            var memberExpresion = invocationDeclaration.Expression as MemberAccessExpressionSyntax;
            var semanticModel = (await document.GetSemanticModelAsync(cancellationToken));
            var variableSymbol = semanticModel.GetSymbolInfo(((IdentifierNameSyntax)memberExpresion.Expression).Identifier.Parent, cancellationToken).Symbol;
            var declaratorSyntax = ((VariableDeclaratorSyntax)variableSymbol.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax());
            var originalDeclarationArgumentList = declaratorSyntax.Initializer.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault().ArgumentList;

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var newArgumentList = originalDeclarationArgumentList.Arguments.Insert(0, invocationDeclaration.ArgumentList.Arguments.FirstOrDefault());
            if (makeCompiled)
            {
                newArgumentList = newArgumentList.Insert(2, SyntaxFactory.Argument(SyntaxFactory.IdentifierName("RegexOptions.Compiled")));
            }

            var isMatchExpression = SyntaxFactory.InvocationExpression(SyntaxFactory.IdentifierName("Regex.IsMatch"),
                SyntaxFactory.ArgumentList(newArgumentList))
                .WithLeadingTrivia(memberExpresion.GetLeadingTrivia())
                .WithTrailingTrivia(memberExpresion.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(invocationDeclaration, isMatchExpression);
            var newDeclaratorSyntax = newRoot.FindToken(declaratorSyntax.SpanStart).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();
            newRoot = newRoot.RemoveNode(newDeclaratorSyntax, SyntaxRemoveOptions.KeepNoTrivia);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private async Task<Document> UseStaticRegexIsMatch(Document document, InvocationExpressionSyntax invocationDeclaration, CancellationToken cancellationToken)
        {
            return await MakeRegexStatic(document, invocationDeclaration, cancellationToken);
        }

        private async Task<Document> UseCompiledAndStaticRegex(Document document, InvocationExpressionSyntax invocationDeclaration, CancellationToken cancellationToken)
        {
            return await MakeRegexStatic(document, invocationDeclaration, cancellationToken, true);
        }

        private async Task<Document> UseCompiledRegex(Document document, InvocationExpressionSyntax invocationDeclaration, CancellationToken cancellationToken)
        {
            var memberExpresion = invocationDeclaration.Expression as MemberAccessExpressionSyntax;
            var semanticModel = (await document.GetSemanticModelAsync(cancellationToken));
            var variableSymbol = semanticModel.GetSymbolInfo(((IdentifierNameSyntax)memberExpresion.Expression).Identifier.Parent, cancellationToken).Symbol;

            var declaratorSyntax = ((VariableDeclaratorSyntax)variableSymbol.DeclaringSyntaxReferences.FirstOrDefault().GetSyntax());
            var originalDeclaration = declaratorSyntax.Initializer.DescendantNodes().OfType<ObjectCreationExpressionSyntax>().FirstOrDefault();

            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var argumentList = originalDeclaration.ArgumentList;
            var newArgumentList = argumentList.AddArguments(SyntaxFactory.Argument(SyntaxFactory.IdentifierName("RegexOptions.Compiled")));

            var newDeclaration = originalDeclaration.WithArgumentList(newArgumentList);

            var newRoot = root.ReplaceNode(originalDeclaration, newDeclaration);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}
