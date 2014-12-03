using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Performance
{
    [ExportCodeFixProvider("CodeCrackerSealedAttributeCodeFixProvider", LanguageNames.CSharp)]
    public class SealedAttributeCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(SealedAttributeAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var sourceSpan = diagnostic.Location.SourceSpan;
            var type = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            context.RegisterFix(
                CodeAction.Create("Mark as sealed", ct => MarkClassAsSealed(context.Document, type, ct)), diagnostic);
        }

        private async Task<Document> MarkClassAsSealed(Document document, ClassDeclarationSyntax type, CancellationToken ct)
        {
            return document
                .WithSyntaxRoot((await document.GetSyntaxRootAsync(ct))
                .ReplaceNode(
                    type,
                    type
                        .WithModifiers(type.Modifiers.Add(SyntaxFactory.Token(SyntaxKind.SealedKeyword)))
                        .WithAdditionalAnnotations(Formatter.Annotation)));
        }
    }
}