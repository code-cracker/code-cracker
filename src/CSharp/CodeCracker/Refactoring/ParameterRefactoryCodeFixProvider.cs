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
            context.RegisterCodeFix(CodeAction.Create("Change to new Class", c => NewClassAsync(context.Document, diagnostic, c), nameof(ParameterRefactoryCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> NewClassAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var oldClass = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ClassDeclarationSyntax>().First();
            var oldNamespace = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<NamespaceDeclarationSyntax>().FirstOrDefault();
            var oldMethod = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First();
            SyntaxNode newRootParameter = null;
            if (oldNamespace == null)
            {
                var newCompilation = AddParameterClassToCompilationUnitAndUpdateClassToUseNamespace((CompilationUnitSyntax)oldClass.Parent, oldClass, oldMethod);
                newRootParameter = root.ReplaceNode(oldClass.Parent, newCompilation);
                return document.WithSyntaxRoot(newRootParameter);
            }
            var newNamespace = AddParameterClassToNamespaceAndUpdateClassToUseNamespace(oldNamespace, oldClass, oldMethod);
            newRootParameter = root.ReplaceNode(oldNamespace, newNamespace).WithAdditionalAnnotations(Formatter.Annotation);
            return document.WithSyntaxRoot(newRootParameter);
        }

        private static List<PropertyDeclarationSyntax> CreateProperties(MethodDeclarationSyntax method)
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
            foreach (ParameterSyntax param in method.ParameterList.Parameters)
            {
                var property = SyntaxFactory.PropertyDeclaration(param.Type, FirstLetteToUpper(param.Identifier.Text))
                    .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PublicKeyword) }))
                    .WithAccessorList(acessorSyntax);
                properties.Add(property);
            }
            return properties;
        }

        private static ClassDeclarationSyntax CreateParameterClass(string newNameClass, MethodDeclarationSyntax oldMethod)
        {
            var properties = CreateProperties(oldMethod);
            return SyntaxFactory.ClassDeclaration(newNameClass)
                .WithMembers(SyntaxFactory.List<MemberDeclarationSyntax>(properties))
                .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)));
        }

        private static NamespaceDeclarationSyntax AddParameterClassToNamespaceAndUpdateClassToUseNamespace(NamespaceDeclarationSyntax oldNamespace, ClassDeclarationSyntax oldClass, MethodDeclarationSyntax oldMethod)
        {
            var className = $"NewClass{oldMethod.Identifier.Text}";
            var newParameterClass = CreateParameterClass(className, oldMethod);
            var newNamespace = oldNamespace.ReplaceNode(oldClass, UpdateClassToUseNewParameterClass(className, oldClass, oldMethod))
                .AddMembers(newParameterClass);
            return newNamespace;
        }

        private static ClassDeclarationSyntax UpdateClassToUseNewParameterClass(string className, ClassDeclarationSyntax classOld, MethodDeclarationSyntax methodOld)
        {
            var newParameter = SyntaxFactory.Parameter(SyntaxFactory.Identifier(FirstLetteToLower(className))).WithType(SyntaxFactory.ParseTypeName(className));
            var parameters = SyntaxFactory.ParameterList(SyntaxFactory.SeparatedList<ParameterSyntax>().Add(newParameter));
            var newMethod = methodOld.WithParameterList(parameters);
            var newClass = classOld.ReplaceNode(methodOld, newMethod);
            return newClass;
        }

        private static CompilationUnitSyntax AddParameterClassToCompilationUnitAndUpdateClassToUseNamespace(CompilationUnitSyntax oldCompilation, ClassDeclarationSyntax oldClass, MethodDeclarationSyntax oldMethod)
        {
            var className = $"NewClass{oldMethod.Identifier.Text}";
            var newParameterClass = CreateParameterClass(className, oldMethod);
            var newNamespace = oldCompilation.ReplaceNode(oldClass, UpdateClassToUseNewParameterClass(className, oldClass, oldMethod))
                .AddMembers(newParameterClass);
            return newNamespace;
        }

        private static string FirstLetteToUpper(string text) =>
            string.Concat(text.Replace(text[0].ToString(), text[0].ToString().ToUpper()));

        private static string FirstLetteToLower(string text) =>
            string.Concat(text.Replace(text[0].ToString(), text[0].ToString().ToLower()));
    }
}