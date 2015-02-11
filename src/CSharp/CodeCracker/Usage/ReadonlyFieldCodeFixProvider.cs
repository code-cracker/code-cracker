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
    [ExportCodeFixProvider("CodeCrackerReadonlyFieldCodeFixProvider", LanguageNames.CSharp), Shared]
    public class ReadonlyFieldCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.ReadonlyField.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var variableDeclarator = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
            if (variableDeclarator != null)
                context.RegisterFix(CodeAction.Create($"Make readonly: '{variableDeclarator.Identifier.Text}'", c => MakeFieldReadonlyAsync(context.Document, variableDeclarator, c)), diagnostic);
        }

        private async Task<Document> MakeFieldReadonlyAsync(Document document, VariableDeclaratorSyntax variable, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            var fieldDeclaration = (FieldDeclarationSyntax)variable.Parent.Parent;
            var newRoot = fieldDeclaration.Declaration.Variables.Count == 1
                ? MakeSingleFieldReadonly(root, fieldDeclaration)
                : MakeMultipleFieldsReadonly(root, fieldDeclaration, variable);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static SyntaxNode MakeSingleFieldReadonly(SyntaxNode root, FieldDeclarationSyntax fieldDeclaration)
        {
            var newFieldDeclaration = fieldDeclaration.AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                .WithTrailingTrivia(fieldDeclaration.GetTrailingTrivia())
                .WithLeadingTrivia(fieldDeclaration.GetLeadingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(fieldDeclaration, newFieldDeclaration);
            return newRoot;
        }
        private static SyntaxNode MakeMultipleFieldsReadonly(SyntaxNode root, FieldDeclarationSyntax fieldDeclaration, VariableDeclaratorSyntax variableToMakeReadonly)
        {
            var newDeclaration = fieldDeclaration.Declaration.RemoveNode(variableToMakeReadonly, SyntaxRemoveOptions.KeepEndOfLine);
            var newFieldDeclaration = fieldDeclaration.WithDeclaration(newDeclaration);
            var newReadonlyFieldDeclaration = fieldDeclaration.WithDeclaration(SyntaxFactory.VariableDeclaration(fieldDeclaration.Declaration.Type, SyntaxFactory.SeparatedList(new[] { variableToMakeReadonly })))
                .WithoutLeadingTrivia()
                .WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia("\n"))
                .AddModifiers(SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword))
                .WithAdditionalAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(fieldDeclaration, new[] { newFieldDeclaration, newReadonlyFieldDeclaration });
            return newRoot;
        }
    }
}