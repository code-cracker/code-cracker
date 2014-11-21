using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using System;
using System.Collections.Generic;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis.CodeActions;
using System.Threading;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Formatting;

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerAutoPropertyCodeFixProvider", LanguageNames.CSharp), Shared]
    public class AutoPropertyCodeFixProvider : CodeFixProvider
    {
        public async override Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var property = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<PropertyDeclarationSyntax>().First();

            context.RegisterFix(
                CodeAction.Create("Use an auto property", c => UseAutoProperty(context.Document, property, c)),
                diagnostic);
        }

        private async Task<Document> UseAutoProperty(Document document, PropertyDeclarationSyntax property, CancellationToken cancellationToken)
        {
            var accessorsList = new List<AccessorDeclarationSyntax>
            {
                SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
            };

            if (property.AccessorList.Accessors.Any(a => a.IsKind(SyntaxKind.SetAccessorDeclaration)))
            {
                var oldGetter = property.AccessorList.Accessors.First(a => a.IsKind(SyntaxKind.SetAccessorDeclaration));

                accessorsList.Add(
                    SyntaxFactory.AccessorDeclaration(
                        SyntaxKind.SetAccessorDeclaration,
                        oldGetter.AttributeLists,
                        oldGetter.Modifiers,
                        null
                    )
                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken)));
            }

            var newProperty = property.WithAccessorList(
                SyntaxFactory.AccessorList(
                    SyntaxFactory.List(accessorsList)))
                    .WithAdditionalAnnotations(Formatter.Annotation);

            var newRoot = await document.GetSyntaxRootAsync();
            newRoot = newRoot.ReplaceNode(property, newProperty);
            return document.WithSyntaxRoot(newRoot);            
        }

        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(AutoPropertyAnalyzer.DiagnosticId);
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}
