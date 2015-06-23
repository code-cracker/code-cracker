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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReadonlyFieldCodeFixProvider)), Shared]
    public class ReadonlyFieldCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(
                DiagnosticId.ReadonlyField.ToDiagnosticId(),
                DiagnosticId.NoPrivateReadonlyField.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(
                $"Make readonly: '{diagnostic.Properties["identifier"]}'", c => MakeFieldReadonlyAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> MakeFieldReadonlyAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var variable = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>().FirstOrDefault();
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