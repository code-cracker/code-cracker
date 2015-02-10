using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
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
        public readonly static string MessageFormat = "Introduce field: {0} from constructor.";

        public sealed override FixAllProvider GetFixAllProvider() => IntroduceFieldFromConstructorCodeFixAllProvider.Instance;

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var parameter = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ParameterSyntax>().First();
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();
            context.RegisterFix(CodeAction.Create(string.Format(MessageFormat, parameter), c => IntroduceFieldFromConstructorAsyncDocument(context.Document, declaration, parameter, c)), diagnostic);
        }
        public async Task<Document> IntroduceFieldFromConstructorAsyncDocument(Document document, ConstructorDeclarationSyntax constructorStatement, ParameterSyntax parameter, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken).ConfigureAwait(false);
            var newRoot = IntroduceFieldFromConstructorAsync(semanticModel, root, constructorStatement, parameter);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return document.WithSyntaxRoot(newRoot);
        }

        public static SyntaxNode IntroduceFieldFromConstructorAsync(SemanticModel semanticModel, SyntaxNode root, ConstructorDeclarationSyntax constructorStatement, ParameterSyntax parameter)
        {
            var oldClass = constructorStatement.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var newClass = oldClass;
            var fieldMembers = oldClass.Members.OfType<FieldDeclarationSyntax>();
            var fieldName = parameter.Identifier.ValueText;

            var fieldVariables = fieldMembers.SelectMany(f => f.Declaration.Variables);
            var existingFieldVariable = fieldVariables.FirstOrDefault(d => d.Identifier.Text == fieldName);
            if (existingFieldVariable != null)
            {
                var variableSymbol = (IFieldSymbol)semanticModel.GetDeclaredSymbol(existingFieldVariable);
                var parameterSymbol = semanticModel.GetDeclaredSymbol(parameter);
                if (!variableSymbol.Type.Equals(parameterSymbol.Type)) existingFieldVariable = null;
            }
            if (existingFieldVariable == null)
            {
                var identifierPostFix = 0;
                while (fieldMembers.Any(p => p.Declaration.Variables.Any(d => d.Identifier.Text == fieldName)))
                    fieldName = parameter.Identifier.ValueText + ++identifierPostFix;
                var newField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                                  .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(fieldName)))))
                                  .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword) }))
                                  .WithAdditionalAnnotations(Formatter.Annotation);
                newClass = newClass.WithMembers(newClass.Members.Insert(0, newField)).WithoutAnnotations(Formatter.Annotation);
            }
            var assignmentField = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                               SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(),
                                               SyntaxFactory.IdentifierName(fieldName)), SyntaxFactory.IdentifierName(parameter.Identifier.ValueText)));
            var newConstructor = constructorStatement.WithBody(constructorStatement.Body.AddStatements(assignmentField));
            newClass = newClass.ReplaceNode(newClass.DescendantNodes().OfType<ConstructorDeclarationSyntax>().First(), newConstructor);
            var newRoot = root.ReplaceNode(oldClass, newClass);
            return newRoot;
        }
    }
}