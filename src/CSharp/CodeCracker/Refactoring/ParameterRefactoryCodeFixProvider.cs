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

namespace CodeCracker.Refactoring
{

    [ExportCodeFixProvider("ParameterRefactoryCodeFixProvider", LanguageNames.CSharp), Shared]
    public class ParameterRefactoryCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() =>
            ImmutableArray.Create(DiagnosticId.ParameterRefactory.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            var diagnosticClass = context.Diagnostics.First();
            var diagnosticSpanClass = diagnosticClass.Location.SourceSpan;
            var declarationClass = root.FindToken(diagnosticSpanClass.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();

            var diagnosticNameSpace = context.Diagnostics.First();
            var diagnosticSpanNameSpace = diagnosticNameSpace.Location.SourceSpan;
            var declarationNameSpace = root.FindToken(diagnosticSpanNameSpace.Start).Parent.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();


            var diagnosticMethod = context.Diagnostics.First();
            var diagnosticSpanMethod = diagnosticMethod.Location.SourceSpan;
            var declarationMethod = root.FindToken(diagnosticSpanClass.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();

            context.RegisterFix(CodeAction.Create("Change to new CLass", c => NewClassAsync(context.Document, declarationNameSpace, declarationClass, declarationMethod, c)), diagnosticClass);

        }

        private async Task<Document> NewClassAsync(Document document, NamespaceDeclarationSyntax OldNameSpace, ClassDeclarationSyntax oldClass, MethodDeclarationSyntax oldMethod, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);
            SyntaxNode newRootParameter = null;


            if (OldNameSpace == null)
            {
                var newCompilation = NewCompilationFactory((CompilationUnitSyntax)oldClass.Parent, oldClass, oldMethod);

                newRootParameter = root.ReplaceNode((CompilationUnitSyntax)oldClass.Parent, newCompilation);

                return document.WithSyntaxRoot(newRootParameter);

            }

            var newNameSpace = NewNameSpaceFactory(OldNameSpace, oldClass, oldMethod);

            newRootParameter = root.ReplaceNode(OldNameSpace, newNameSpace);

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


            var propertys = new List<PropertyDeclarationSyntax>();

            foreach (ParameterSyntax param in methodOld.ParameterList.Parameters)
            {
                var property = SyntaxFactory.PropertyDeclaration(
                                default(SyntaxList<AttributeListSyntax>),
                                SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }),
                                param.Type,
                                default(ExplicitInterfaceSpecifierSyntax),
                                SyntaxFactory.Identifier(FirstLetteToUpper(param.Identifier.Text)),
                                acessorSyntax);

                propertys.Add(property);

            }

            return propertys;
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

        private static string FirstLetteToUpper(string text)
        {
            return string.Concat(text.Replace(text[0].ToString(), text[0].ToString().ToUpper()));
        }

        private static string FirstLetteToLower(string text)
        {
            return string.Concat(text.Replace(text[0].ToString(), text[0].ToString().ToLower()));
        }
    }
}