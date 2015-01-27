using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Refactoring
{
    public abstract class BaseAllowMembersOrderingCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.AllowMembersOrdering.ToDiagnosticId());

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        protected readonly string codeActionDescription;

        protected BaseAllowMembersOrderingCodeFixProvider(string codeActionDescription) : base()
        {
            this.codeActionDescription = codeActionDescription;
        }

        public override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            var typeDeclarationSyntax = root
                        .FindToken(diagnosticSpan.Start)
                        .Parent as TypeDeclarationSyntax;

            var newDocument = await AllowMembersOrderingAsync(context.Document, typeDeclarationSyntax, context.CancellationToken);
            if (newDocument != null)
                context.RegisterFix(CodeAction.Create(string.Format(codeActionDescription, typeDeclarationSyntax.Identifier.ValueText), newDocument), diagnostic);
        }

        private async Task<Document> AllowMembersOrderingAsync(Document document, TypeDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
        {
            var membersDeclaration =
                typeDeclarationSyntax
                    .ChildNodes()
                    .OfType<MemberDeclarationSyntax>();

            var root = await document.GetSyntaxRootAsync(cancellationToken) as CompilationUnitSyntax;

            TypeDeclarationSyntax newTypeDeclarationSyntax;
            var orderChanged = TryReplaceTypeMembers(
                typeDeclarationSyntax,
                membersDeclaration,
                membersDeclaration.OrderBy(member => member, GetMemberDeclarationComparer(document, cancellationToken)),
                out newTypeDeclarationSyntax);

            if (!orderChanged) return null;

            var newDocument = document.WithSyntaxRoot(root
                 .ReplaceNode(typeDeclarationSyntax, newTypeDeclarationSyntax)
                 .WithAdditionalAnnotations(Formatter.Annotation)
            );

            return newDocument;
        }

        protected abstract IComparer<MemberDeclarationSyntax> GetMemberDeclarationComparer(Document document, CancellationToken cancellationToken);

        private bool TryReplaceTypeMembers(TypeDeclarationSyntax typeDeclarationSyntax, IEnumerable<MemberDeclarationSyntax> membersDeclaration, IEnumerable<MemberDeclarationSyntax> sortedMembers, out TypeDeclarationSyntax orderedType)
        {
            var sortedMembersQueue = new Queue<MemberDeclarationSyntax>(sortedMembers);
            var orderChanged = false;

            orderedType = typeDeclarationSyntax.ReplaceNodes(
                membersDeclaration,
                (original, rewritten) =>
                {
                    var newMember = sortedMembersQueue.Dequeue();
                    if (!orderChanged && !original.Equals(newMember)) orderChanged = true;
                    return newMember;
                });
            return orderChanged;
        }
    }
}