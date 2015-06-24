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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name =nameof(IntroduceFieldFromConstructorCodeFixProvider)), Shared]
    public class IntroduceFieldFromConstructorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.IntroduceFieldFromConstructor.ToDiagnosticId());
        public readonly static string MessageFormat = "Introduce field: {0} from constructor.";

        public sealed override FixAllProvider GetFixAllProvider() => IntroduceFieldFromConstructorCodeFixAllProvider.Instance;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(
                string.Format(MessageFormat, diagnostic.Properties["parameterName"]), c => IntroduceFieldFromConstructorDocumentAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        public async static Task<Document> IntroduceFieldFromConstructorDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var parameter = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ParameterSyntax>().First();
            var constructor = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();
            var newRoot = IntroduceFieldFromConstructor(root, constructor, parameter);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return document.WithSyntaxRoot(newRoot);
        }

        public static SyntaxNode IntroduceFieldFromConstructor(SyntaxNode root, ConstructorDeclarationSyntax constructorStatement, ParameterSyntax parameter)
        {
            var oldClass = constructorStatement.FirstAncestorOrSelf<ClassDeclarationSyntax>();
            var newClass = oldClass;
            var fieldName = parameter.Identifier.ValueText;
            var fieldType = parameter.Type;
            var members = ExtractMembersFromClass(oldClass.Members);

            if (!members.Any(p => p.Key == fieldName && p.Value == fieldType.ToString()))
            {
                var identifierPostFix = 0;
                while (members.Any(p => p.Key == fieldName))
                    fieldName = parameter.Identifier.ValueText + ++identifierPostFix;
                var newField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(parameter.Type)
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

        private static Dictionary<string, string> ExtractMembersFromClass(SyntaxList<MemberDeclarationSyntax> classMembers)
        {
            var members = new Dictionary<string, string>();
            foreach (var m in classMembers)
            {
                var name = "";
                if (m.IsKind(SyntaxKind.MethodDeclaration))
                {
                    var eve = m as MethodDeclarationSyntax;
                    name = eve.Identifier.Text;
                }
                if (m.IsKind(SyntaxKind.EventDeclaration))
                {
                    var theEvent = m as EventDeclarationSyntax;
                    name = theEvent.Identifier.Text;
                }
                if (m.IsKind(SyntaxKind.EventFieldDeclaration))
                {
                    var eventField = m as EventFieldDeclarationSyntax;
                    foreach (var v in eventField.Declaration.Variables)
                    {
                        members.Add(v.Identifier.Text, eventField.Declaration.Type.ToString());
                    }
                }
                if (m.IsKind(SyntaxKind.FieldDeclaration))
                {
                    var field = m as FieldDeclarationSyntax;
                    foreach (var v in field.Declaration.Variables)
                    {
                        members.Add(v.Identifier.Text, field.Declaration.Type.ToString());
                    }
                }
                if (m.IsKind(SyntaxKind.PropertyDeclaration))
                {
                    var property = m as PropertyDeclarationSyntax;
                    name = property.Identifier.Text;
                }
                if (m.IsKind(SyntaxKind.DelegateDeclaration))
                {
                    var theDelegate = m as DelegateDeclarationSyntax;
                    name = theDelegate.Identifier.Text;
                }
                if (m.IsKind(SyntaxKind.ClassDeclaration, SyntaxKind.StructDeclaration, SyntaxKind.EnumDeclaration, SyntaxKind.InterfaceDeclaration))
                {
                    var type = m as BaseTypeDeclarationSyntax;
                    name = type.Identifier.Text;
                }
                if (name != "")
                {
                    members.Add(name, "");
                }
            }
            return members;
        }
    }
}