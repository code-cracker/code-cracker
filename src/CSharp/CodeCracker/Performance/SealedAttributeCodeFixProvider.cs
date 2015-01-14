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
    public class SealedAttributeCodeFixProvider : CodeFix
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.SealedAttributeAnalyzer.ToDiagnosticId());

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var type = await context.GetNodeInPositionAsync<ClassDeclarationSyntax>(diagnostic.Location.SourceSpan);
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