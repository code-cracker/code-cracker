using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Extensions;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Usage
{
    [ExportCodeFixProvider(nameof(DisposableFieldNotDisposedCodeFixProvider), LanguageNames.CSharp), Shared]
    public class DisposableFieldNotDisposedCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var variableDeclarators = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<VariableDeclaratorSyntax>();
            foreach (var variableDeclarator in variableDeclarators)
                context.RegisterCodeFix(CodeAction.Create($"Dispose field '{variableDeclarator.Identifier.Value}'", c => DisposeFieldAsync(context.Document, variableDeclarator, c)), diagnostic);
        }

        private async Task<Document> DisposeFieldAsync(Document document, VariableDeclaratorSyntax variableDeclarator, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var type = variableDeclarator.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            var typeSymbol = semanticModel.GetDeclaredSymbol(type);
            var newTypeImplementingIDisposable = AddIDisposableImplementationToType(type, typeSymbol);
            var newTypeWithDisposeMethod = AddDisposeDeclarationToDisposeMethod(variableDeclarator, newTypeImplementingIDisposable, typeSymbol);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(type, newTypeWithDisposeMethod);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static TypeDeclarationSyntax AddIDisposableImplementationToType(TypeDeclarationSyntax type, INamedTypeSymbol typeSymbol)
        {
            var iDisposableInterface = typeSymbol.AllInterfaces.FirstOrDefault(i => i.ToString() == "System.IDisposable");
            if (iDisposableInterface != null) return type;
            var newBaseList = type.BaseList != null
                ? type.BaseList.AddTypes(SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseName("System.IDisposable").WithAdditionalAnnotations(Simplifier.Annotation)))
                : SyntaxFactory.BaseList(SyntaxFactory.SeparatedList(new BaseTypeSyntax[] {
                        SyntaxFactory.SimpleBaseType(SyntaxFactory.ParseName("System.IDisposable").WithAdditionalAnnotations(Simplifier.Annotation)) }));
            TypeDeclarationSyntax newType = ((dynamic)type)
                .WithBaseList(newBaseList)
                .WithIdentifier(SyntaxFactory.Identifier(type.Identifier.Text));//this line is stupid, it is here only to remove the line break at the end of the identifier that roslyn for some reason puts there
            newType = newType.WithAdditionalAnnotations(Formatter.Annotation);//can't chain because this would be an ext.method on a dynamic type
            return newType;
        }

        private static TypeDeclarationSyntax AddDisposeDeclarationToDisposeMethod(VariableDeclaratorSyntax variableDeclarator, TypeDeclarationSyntax type, INamedTypeSymbol typeSymbol)
        {
            var disposableMethod = typeSymbol.GetMembers("Dispose").OfType<IMethodSymbol>().FirstOrDefault(d => d.Arity == 0);
            var disposeStatement = SyntaxFactory.ParseStatement($"{variableDeclarator.Identifier.ToString()}.Dispose();");
            TypeDeclarationSyntax newType;
            if (disposableMethod == null)
            {
                var disposeMethod = SyntaxFactory.MethodDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.VoidKeyword)), "Dispose")
                      .WithModifiers(SyntaxFactory.TokenList(SyntaxFactory.Token(SyntaxKind.PublicKeyword)))
                      .WithBody(SyntaxFactory.Block(disposeStatement))
                      .WithAdditionalAnnotations(Formatter.Annotation);
                newType = ((dynamic)type).AddMembers(disposeMethod);
            }
            else
            {
                var existingDisposeMethod = (MethodDeclarationSyntax)disposableMethod.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax();
                if (type.Members.Contains(existingDisposeMethod))
                {
                    var newDisposeMethod = existingDisposeMethod.AddBodyStatements(disposeStatement)
                        .WithAdditionalAnnotations(Formatter.Annotation);
                    newType = type.ReplaceNode(existingDisposeMethod, newDisposeMethod);
                }
                else
                {
                    //we will simply anotate the code for now, but ideally we would change another document
                    //for this to work we have to be able to fix more than one doc
                    var fieldDeclaration = variableDeclarator.Parent.Parent;
                    var newFieldDeclaration = fieldDeclaration.WithTrailingTrivia(SyntaxFactory.ParseTrailingTrivia($"//add {disposeStatement.ToString()} to the Dispose method on another file.").AddRange(fieldDeclaration.GetTrailingTrivia()))
                        .WithLeadingTrivia(fieldDeclaration.GetLeadingTrivia());
                    newType = type.ReplaceNode(fieldDeclaration, newFieldDeclaration);
                }
            }
            return newType;
        }
    }
}