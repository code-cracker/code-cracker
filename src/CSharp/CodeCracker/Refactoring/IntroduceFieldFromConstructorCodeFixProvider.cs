                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Refactoring
{
    [ExportCodeFixProvider("CodeCrackerIntroduceFieldFromConstructorCodeFixProvider", LanguageNames.CSharp), Shared]
    public class IntroduceFieldFromConstructorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() => ImmutableArray.Create(DiagnosticId.IntroduceFieldFromConstructor.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var parameterName = diagnostic.GetMessage().Split(':')[1].Trim();
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();
            context.RegisterFix(CodeAction.Create($"Introduce field: {parameterName} from constructor.", c => IntroduceFieldFromConstructorAsync(context.Document, declaration, parameterName, c)), diagnostic);
        }

        private async Task<Document> IntroduceFieldFromConstructorAsync(Document document, ConstructorDeclarationSyntax constructorStatement, string parameterName, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var oldClass = constructorStatement.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var fieldMembers = oldClass.Members.OfType<FieldDeclarationSyntax>();
            var fieldName = parameterName;

            if(fieldMembers.Any(p => p.Declaration.Variables.First().Identifier.Text == fieldName))
            {
                fieldName += "1";
            }

            var assignmentField = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                               SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(),
                                               SyntaxFactory.IdentifierName(fieldName)), SyntaxFactory.IdentifierName(parameterName)));

            var newField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                              .WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(fieldName)))))
                              .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword) }))
                              .WithAdditionalAnnotations(Formatter.Annotation);

            var newConstructor = constructorStatement.WithBody(constructorStatement.Body.AddStatements(assignmentField));
            var newClassConstructor = oldClass.ReplaceNode(constructorStatement, newConstructor);
            var newClass = newClassConstructor.WithMembers(newClassConstructor.Members.Insert(0, newField))
                           .WithoutAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(oldClass, newClass);
            return document.WithSyntaxRoot(newRoot);
        }
    }                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                           using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Rename;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.Refactoring
{
    [ExportCodeFixProvider("CodeCrackerIntroduceFieldFromConstructorCodeFixProvider", LanguageNames.CSharp), Shared]
    public class IntroduceFieldFromConstructorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds() => ImmutableArray.Create(DiagnosticId.IntroduceFieldFromConstructor.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var parameterName = diagnostic.GetMessage().Split(':')[1].Trim();
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();
            context.RegisterFix(CodeAction.Create($"Introduce field: {parameterName} from constructor.", c => IntroduceFieldFromConstructorAsync(context.Document, declaration, parameterName, c)), diagnostic);
        }

        private async Task<Document> IntroduceFieldFromConstructorAsync(Document document, ConstructorDeclarationSyntax constructorStatement, string parameterName, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken);

            var oldClass = constructorStatement.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var fieldMembers = oldClass.Members.OfType<FieldDeclarationSyntax>();
            var fieldName = parameterName;

            if(fieldMembers.Any(p => p.Declaration.Variables.First().Identifier.Text == fieldName))
            {
                fieldName += "1";
            }

            var assignmentField = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                               SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(),
                                               SyntaxFactory.IdentifierName(fieldName)), SyntaxFactory.IdentifierName(parameterName)));

            var newField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                              .WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(fieldName)))))
                              .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword) }))
                              .WithAdditionalAnnotations(Formatter.Annotation);

            var newConstructor = constructorStatement.WithBody(constructorStatement.Body.AddStatements(assignmentField));
            var newClassConstructor = oldClass.ReplaceNode(constructorStatement, newConstructor);
            var newClass = newClassConstructor.WithMembers(newClassConstructor.Members.Insert(0, newField))
                           .WithoutAnnotations(Formatter.Annotation);
            var newRoot = root.ReplaceNode(oldClass, newClass);
            return document.WithSyntaxRoot(newRoot);
        }
    }
}
}