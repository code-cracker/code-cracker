using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Refactoring
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ParameterRefactoryCodeFixProvider)), Shared]
    public class ParameterRefactoryCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ParameterRefactory.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Change to new Class", c => NewClassAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async Task<Document> NewClassAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var oldClass = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
            var oldNamespace = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var oldMethod = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            SyntaxNode newRootParameter = null;
            if (oldNamespace == null)
            {
                var newCompilation = NewCompilationFactory((CompilationUnitSyntax)oldClass.Parent, oldClass, oldMethod);
                newRootParameter = root.ReplaceNode(oldClass.Parent, newCompilation);
                return document.WithSyntaxRoot(newRootParameter);
            }
            var newNameSpace = NewNameSpaceFactory(oldNamespace, oldClass, oldMethod);
            newRootParameter = root.ReplaceNode(oldNamespace, newNameSpace);
            return document.WithSyntaxRoot(newRootParameter);
        }

        private static List<PropertyDeclarationSyntax> NewPropertyClassFactory(MethodDeclarationSyntax methodOld)
        {
            var newGetSyntax = SyntaxFactory.AccessorDeclaration(SyntaxKind.GetAccessorDeclaration)
                                           .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            var newSetSyntax = SyntaxFactory.AccessorDeclaration(SyntaxKind.SetAccessorDeclaration)
                                            .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken));
            var acessorSyntax = SyntaxFactory.AccessorList(
                                SyntaxFactory.Token(SyntaxKind.OpenBraceToken),
                                SyntaxFactory.List(new[] { newGetSyntax, newSetSyntax }),
                                SyntaxFactory.Token(SyntaxKind.CloseBraceToken));
            var properties = new List<PropertyDeclarationSyntax>();
            foreach (ParameterSyntax param in methodOld.ParameterList.Parameters)
            {
                var property = SyntaxFactory.PropertyDeclaration(
                                default(SyntaxList<AttributeListSyntax>),
                                SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }),
                                param.Type,
                                default(ExplicitInterfaceSpecifierSyntax),
                                SyntaxFactory.Identifier(FirstLetteToUpper(param.Identifier.Text)),
                                acessorSyntax);
                properties.Add(property);

            }
            return properties;
        }

        private static ClassDeclarationSyntax NewClassParameterFactory(string newNameClass, List<PropertyDeclarationSyntax> Property)
        {
            return SyntaxFactory.ClassDeclaration(newNameClass)
                                                  .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(Property))
                                                  .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                                                  .WithAdditionalAnnotations(Formatter.Annotation);

        }

        private static NamespaceDeclarationSyntax NewNameSpaceFactory(NamespaceDeclarationSyntax OldNameSpace, ClassDeclarationSyntax OldClass, MethodDeclarationSyntax OldMethod)
        {
            var newNameSpace = OldNameSpace;
            var className = $"NewClass{OldMethod.Identifier.Text}";
            var memberNameSpaceOld = (from member in OldNameSpace.Members
                                      where member == OldClass
                                      select member).FirstOrDefault();
            newNameSpace = OldNameSpace.ReplaceNode(memberNameSpaceOld, NewClassFactory(className, OldClass, OldMethod));
            var newParameterClass = NewClassParameterFactory(className, NewPropertyClassFactory(OldMethod));
            newNameSpace = newNameSpace
                            .WithMembers(newNameSpace.Members.Add(newParameterClass))
                            .WithAdditionalAnnotations(Formatter.Annotation);
            return newNameSpace;
        }

        private static ClassDeclarationSyntax NewClassFactory(string className, ClassDeclarationSyntax classOld, MethodDeclarationSyntax methodOld)
        {

            var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier($"{className} {FirstLetteToLower(className)}"));

            var paremeters = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>().Add(newParameter))
                .WithAdditionalAnnotations(Formatter.Annotation);


            var newMethod = SyntaxFactory.MethodDeclaration(methodOld.ReturnType, methodOld.Identifier.Text)
                        .WithModifiers(methodOld.Modifiers)
                        .WithParameterList(paremeters)
                        .WithBody(methodOld.Body)
                        .WithAdditionalAnnotations(Formatter.Annotation);

            var newClass = classOld.ReplaceNode(methodOld, newMethod);

            return newClass;
        }

        private static CompilationUnitSyntax NewCompilationFactory(CompilationUnitSyntax OldCompilation, ClassDeclarationSyntax OldClass, MethodDeclarationSyntax OldMethod)
        {
            var newNameSpace = OldCompilation;
            var className = $"NewClass{OldMethod.Identifier.Text}";
            var OldMemberNameSpace = (from member in OldCompilation.Members
                                      where member == OldClass
                                      select member).FirstOrDefault();
            newNameSpace = OldCompilation.ReplaceNode(OldMemberNameSpace, NewClassFactory(className, OldClass, OldMethod));
            var newParameterClass = NewClassParameterFactory(className, NewPropertyClassFactory(OldMethod));
            return newNameSpace.WithMembers(newNameSpace.Members.Add(newParameterClass))
                                .WithAdditionalAnnotations(Formatter.Annotation);

        }

        private static string FirstLetteToUpper(string text) =>
            string.Concat(text.Replace(text[0].ToString(), text[0].ToString().ToUpper()));

        private static string FirstLetteToLower(string text) =>
            string.Concat(text.Replace(text[0].ToString(), text[0].ToString().ToLower()));
    }
}