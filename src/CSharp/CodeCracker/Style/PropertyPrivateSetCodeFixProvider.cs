﻿using Microsoft.CodeAnalysis;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PropertyPrivateSetCodeFixProvider)), Shared]
    public class PropertyPrivateSetCodeFixProvider : CodeFixProvider
    {
        private enum FixType
        {
            PrivateFix,
            ProtectedFix
        }

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.PropertyPrivateSet.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();
            context.RegisterCodeFix(CodeAction.Create("Change property to 'private set'", c => ChangePropertySetAsync(context.Document, declaration, c, FixType.PrivateFix)), diagnostic);
            context.RegisterCodeFix(CodeAction.Create("Change property to 'protected set'", c => ChangePropertySetAsync(context.Document, declaration, c, FixType.ProtectedFix)), diagnostic);
        }

        private async Task<Document> ChangePropertySetAsync(Document document, PropertyDeclarationSyntax propertyStatement, CancellationToken cancellationToken, FixType fixType)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var getAcessor = (propertyStatement.AccessorList.Accessors[0].Keyword.Text == "get") ? propertyStatement.AccessorList.Accessors[0] : propertyStatement.AccessorList.Accessors[1];
            var setAcessor = (propertyStatement.AccessorList.Accessors[0].Keyword.Text == "set") ? propertyStatement.AccessorList.Accessors[0] : propertyStatement.AccessorList.Accessors[1];

            var privateprotectedModifier = SyntaxFactory.Token(fixType == FixType.PrivateFix ? SyntaxKind.PrivateKeyword : SyntaxKind.ProtectedKeyword)
                .WithAdditionalAnnotations(Formatter.Annotation);

            var modifiers = setAcessor.Modifiers.Add(privateprotectedModifier);
            setAcessor = setAcessor.WithModifiers(modifiers);

            var newProperty = SyntaxFactory.PropertyDeclaration(propertyStatement.Type, propertyStatement.Identifier)
                .WithModifiers(propertyStatement.Modifiers)
                .WithAccessorList(SyntaxFactory.AccessorList(SyntaxFactory.List<AccessorDeclarationSyntax>(new AccessorDeclarationSyntax[] { getAcessor, setAcessor })))
                .WithLeadingTrivia(propertyStatement.GetLeadingTrivia()).WithTrailingTrivia(propertyStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(propertyStatement, newProperty);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}