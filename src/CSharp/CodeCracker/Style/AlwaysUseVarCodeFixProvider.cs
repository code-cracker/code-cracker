using System.Collections.Generic;
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

namespace CodeCracker.CSharp.Style
{

    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(AlwaysUseVarCodeFixProvider)), Shared]
    public class AlwaysUseVarCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.AlwaysUseVar.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Use 'var'", c => UseVarAsync(context.Document, diagnostic, c), nameof(AlwaysUseVarCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> UseVarAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var localDeclaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LocalDeclarationStatementSyntax>().First();
            var variableDeclaration = localDeclaration.Declaration;
            var numVariables = variableDeclaration.Variables.Count;

            var newVariableDeclarations = new VariableDeclarationSyntax[numVariables];

            var varSyntax = SyntaxFactory.IdentifierName("var");

            //Create a new var declaration for each variable.
            for (var i = 0; i < numVariables; i++)
            {
                var originalVariable = variableDeclaration.Variables[i];
                var newLeadingTrivia = originalVariable.GetLeadingTrivia();
            
                //Get the trivia from the separator as well
                if (i != 0)
                {
                    newLeadingTrivia = newLeadingTrivia.InsertRange(0,
                        variableDeclaration.Variables.GetSeparator(i-1).GetAllTrivia());
                }

                newVariableDeclarations[i] = SyntaxFactory.VariableDeclaration(varSyntax)
                    .AddVariables(originalVariable.WithLeadingTrivia(newLeadingTrivia));
            }

            //ensure trivia for leading type node is preserved
            var originalTypeSyntax = variableDeclaration.Type;
            newVariableDeclarations[0] = newVariableDeclarations[0]
                .WithType((TypeSyntax) varSyntax.WithSameTriviaAs(originalTypeSyntax));

            var newLocalDeclarationStatements = newVariableDeclarations.Select(SyntaxFactory.LocalDeclarationStatement).ToList();


            //Preserve the trivia for the entire statement at the start and at the end
            newLocalDeclarationStatements[0] =
                newLocalDeclarationStatements[0].WithLeadingTrivia(variableDeclaration.GetLeadingTrivia());
            var lastIndex = numVariables -1;
            newLocalDeclarationStatements[lastIndex] =
                newLocalDeclarationStatements[lastIndex].WithTrailingTrivia(localDeclaration.GetTrailingTrivia());

            var newRoot = root.ReplaceNode(localDeclaration, newLocalDeclarationStatements);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}