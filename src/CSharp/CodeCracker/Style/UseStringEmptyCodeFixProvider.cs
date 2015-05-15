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

namespace CodeCracker.CSharp.Style
{

    [ExportCodeFixProvider("CodeCrackerUseStringEmptyCodeFixProvider", LanguageNames.CSharp), Shared]
    public class UseStringEmptyCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.UseStringEmpty.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var localDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<VariableDeclarationSyntax>().First();
            const string message = "Use 'String.Empty'";
            context.RegisterCodeFix(CodeAction.Create(message, c => UseStringEmptyAsync(context.Document, localDeclaration, c)), diagnostic);
        }

        private async Task<Document> UseStringEmptyAsync(Document document, VariableDeclarationSyntax variableDeclaration, CancellationToken cancellationToken)
        {
            var variableName = variableDeclaration.Variables.First().Identifier.Text;

            var newVariable = SyntaxFactory.VariableDeclaration(variableDeclaration.Type)
                                    .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(variableName))
                                    .WithInitializer(SyntaxFactory.EqualsValueClause(SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression,
                                        SyntaxFactory.IdentifierName(@"String"), SyntaxFactory.IdentifierName(@"Empty"))))))
                                    .WithAdditionalAnnotations(Formatter.Annotation);

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = root.ReplaceNode(variableDeclaration, newVariable);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}