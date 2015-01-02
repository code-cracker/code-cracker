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
        protected readonly string codeActionDescription;

        protected BaseAllowMembersOrderingCodeFixProvider(string codeActionDescription) :
            base()
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

            context.RegisterFix(
                CodeAction.Create(
                    codeActionDescription,
                    cancellationToken => AllowMembersOrderingAsync(context.Document, typeDeclarationSyntax, cancellationToken)),
                    diagnostic);
        }

        private async Task<Document> AllowMembersOrderingAsync(Document document, TypeDeclarationSyntax typeDeclarationSyntax, CancellationToken cancellationToken)
        {
            var membersDeclaration =
                typeDeclarationSyntax
                    .ChildNodes()
                    .OfType<MemberDeclarationSyntax>();

            var root = await document.GetSyntaxRootAsync(cancellationToken) as CompilationUnitSyntax;

            var newTypeDeclarationSyntax = ReplaceTypeMembers(
                typeDeclarationSyntax,
                membersDeclaration,
                membersDeclaration.OrderBy(member => member, GetMemberDeclarationComparer(document, cancellationToken)));

            var newDocument = document.WithSyntaxRoot(root
                 .ReplaceNode(typeDeclarationSyntax, newTypeDeclarationSyntax)
                 .WithAdditionalAnnotations(Formatter.Annotation)
            );

            return newDocument;
        }

        protected abstract IComparer<MemberDeclarationSyntax> GetMemberDeclarationComparer(Document document, CancellationToken cancellationToken);

        private TypeDeclarationSyntax ReplaceTypeMembers(TypeDeclarationSyntax typeDeclarationSyntax, IEnumerable<MemberDeclarationSyntax> membersDeclaration, IEnumerable<MemberDeclarationSyntax> sortedMembers)
        {
            var sortedMembersQueue = new Queue<MemberDeclarationSyntax>(sortedMembers);

            return typeDeclarationSyntax.ReplaceNodes(
                membersDeclaration,
                (original, rewritten) => sortedMembersQueue.Dequeue());
        }

        public override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(AllowMembersOrderingAnalyzer.DiagnosticId);
        }

        public override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }
    }
}