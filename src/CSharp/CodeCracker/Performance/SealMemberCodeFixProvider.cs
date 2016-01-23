using CodeCracker.Properties;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.FindSymbols;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Performance
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(SealMemberCodeFixProvider)), Shared]
    public class SealMemberCodeFixProvider : CodeFixProvider
    {
        public override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.SealMember.ToDiagnosticId());

        public override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public async sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var document = context.Document;
            var cancellationToken = context.CancellationToken;
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var classDeclaration = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().FirstOrDefault();
            List<ISymbol> membersToFix;
            string title;
            if (diagnostic.Properties["kind"] == "class")
            {
                membersToFix = await GetSealableMembersAsync(document, classDeclaration, cancellationToken);
                title = string.Format(Resources.SealMemberCodeFixProvider_ClassTitle, classDeclaration.Identifier.ValueText);
            }
            else
            {
                var member = root.FindToken(diagnostic.Location.SourceSpan.Start).Parent.AncestorsAndSelf().OfType<MemberDeclarationSyntax>().FirstOrDefault();
                membersToFix = await GetSealableMembersAsync(document, classDeclaration, cancellationToken, member);
                title = string.Format(Resources.SealMemberCodeFixProvider_MemberTitle, SealMemberAnalyzer.GetIdentifierValue(member));
            }
            if (!membersToFix.Any()) return;
            context.RegisterCodeFix(CodeAction.Create(title,
                c => MakeSealedAsync(context.Document, root, classDeclaration, membersToFix, cancellationToken),
                nameof(SealMemberCodeFixProvider)), diagnostic);
        }

        private static async Task<List<ISymbol>> GetSealableMembersAsync(Document document, ClassDeclarationSyntax classDeclaration, CancellationToken cancellationToken, MemberDeclarationSyntax onlyMemberToFix = null)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var classSymbol = semanticModel.GetDeclaredSymbol(classDeclaration, cancellationToken);
            var onlyMemberToFixSymbol = GetSymbolForMember(onlyMemberToFix, semanticModel);
            var sealableMemberCandidates = onlyMemberToFixSymbol != null
                ? new[] { classSymbol.GetMembers().First(member => member.Equals(onlyMemberToFixSymbol)) }.ToList()
                : classSymbol.GetMembers().Where(member => member.IsOverride && !member.IsSealed).ToList();
            var notSealableMembers = await Task.WhenAll(
                from sealableMemberCandidate in sealableMemberCandidates
                let overridesFoundTask = SymbolFinder.FindOverridesAsync(sealableMemberCandidate, document.Project.Solution, cancellationToken: cancellationToken)
                select overridesFoundTask.ContinueWith(t => t.Result.Any() ? sealableMemberCandidate : null, cancellationToken));
            return sealableMemberCandidates.Except(notSealableMembers).ToList();
        }

        private static ISymbol GetSymbolForMember(MemberDeclarationSyntax member, SemanticModel semanticModel)
        {
            ISymbol memberSymbol = null;
            if (member != null)
            {
                if (member.IsKind(SyntaxKind.EventFieldDeclaration))
                {
                    var eventVariable = ((EventFieldDeclarationSyntax)member).Declaration.Variables[0];
                    memberSymbol = semanticModel.GetDeclaredSymbol(eventVariable);
                }
                else
                {
                    memberSymbol = semanticModel.GetDeclaredSymbol(member);
                }
            }
            return memberSymbol;
        }

        private static readonly SyntaxToken sealedSyntaxToken = SyntaxFactory.Token(SyntaxKind.SealedKeyword);
        private async static Task<Document> MakeSealedAsync(Document document, SyntaxNode root, ClassDeclarationSyntax classDeclaration,
            IList<ISymbol> membersToFix, CancellationToken cancellationToken)
        {
            var memberReferences = await Task.WhenAll(membersToFix.SelectMany(m => m.DeclaringSyntaxReferences)
                .Select(sr => sr.GetSyntaxAsync(cancellationToken)));
            var overridenMembers = memberReferences.Where(m => m.FirstAncestorOfType<ClassDeclarationSyntax>().Equals(classDeclaration))
                .Select(n => n.FirstAncestorOrSelfOfType(typeof(PropertyDeclarationSyntax),
                    typeof(MethodDeclarationSyntax), typeof(EventDeclarationSyntax), typeof(EventFieldDeclarationSyntax)))
                .Cast<MemberDeclarationSyntax>();
            var newMembers = overridenMembers.ToDictionary(m => m, m => m.AddModifiers(sealedSyntaxToken)
                .WithAdditionalAnnotations(Formatter.Annotation));
            var newClassDeclaration = classDeclaration.ReplaceNodes(overridenMembers, (member, _) => newMembers[member]);
            var newRoot = root.ReplaceNode(classDeclaration, newClassDeclaration);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}