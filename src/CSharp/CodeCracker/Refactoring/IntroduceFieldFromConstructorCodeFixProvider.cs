using CodeCracker.FixAllProviders;
using CodeCracker.Properties;
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
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(IntroduceFieldFromConstructorCodeFixProvider)), Shared]
    public sealed class IntroduceFieldFromConstructorCodeFixProvider : CodeFixProvider, IFixDocumentInternalsOnly
    {
        private static readonly FixAllProvider FixAllProvider = new DocumentCodeFixProviderAll(Resources.IntroduceFieldFromConstructorCodeFixProvider_Title);
        private static readonly string MessageFormat = Resources.IntroduceFieldFromConstructorCodeFixProvider_MessageFormat;

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(DiagnosticId.IntroduceFieldFromConstructor.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => FixAllProvider;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create(
                string.Format(MessageFormat, diagnostic.Properties["parameterName"]), c => IntroduceFieldFromConstructorDocumentAsync(context.Document, diagnostic, c), nameof(IntroduceFieldFromConstructorCodeFixProvider)), diagnostic);
            return Task.FromResult(0);
        }

        public async Task<Document> FixDocumentAsync(SyntaxNode nodeWithDiagnostic, Document document, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var parameter = nodeWithDiagnostic.AncestorsAndSelf().OfType<ParameterSyntax>().First();
            var constructor = nodeWithDiagnostic.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();
            var newRoot = IntroduceFieldFromConstructor(root, constructor, parameter);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return document.WithSyntaxRoot(newRoot);
        }

        public async Task<Document> IntroduceFieldFromConstructorDocumentAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var nodeWithDiagnostic = root.FindToken(diagnosticSpan.Start).Parent;
            return await FixDocumentAsync(nodeWithDiagnostic, document, cancellationToken);
        }

        public static SyntaxNode IntroduceFieldFromConstructor(SyntaxNode root, ConstructorDeclarationSyntax constructorStatement, ParameterSyntax parameter)
        {
            // There are no constructors in interfaces, therefore all types remaining type (class and struct) are fine.
            var oldType = constructorStatement.FirstAncestorOrSelf<TypeDeclarationSyntax>();
            var newType = oldType;
            var fieldName = parameter.Identifier.ValueText;
            var fieldType = parameter.Type;
            var members = ExtractMembersFromClass(oldType.Members);

            var addMember = false;
            if (!members.Any(p => p.Key == fieldName && p.Value == fieldType.ToString()))
            {
                var identifierPostFix = 0;
                while (members.Any(p => p.Key == fieldName))
                    fieldName = parameter.Identifier.ValueText + ++identifierPostFix;

                addMember = true;
            }

            var assignmentField = SyntaxFactory.ExpressionStatement(SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                               SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(),
                                               SyntaxFactory.IdentifierName(fieldName)), SyntaxFactory.IdentifierName(parameter.Identifier.ValueText)));
            var newConstructor = constructorStatement.WithBody(constructorStatement.Body.AddStatements(assignmentField));
            newType = newType.ReplaceNode(constructorStatement, newConstructor);

            if (addMember)
            {
                var newField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(parameter.Type)
                                    .WithVariables(SyntaxFactory.SingletonSeparatedList(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(fieldName)))))
                                    .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword) }))
                                    .WithAdditionalAnnotations(Formatter.Annotation);
                newType = newType.WithMembers(newType.Members.Insert(0, newField)).WithoutAnnotations(Formatter.Annotation);
            }
            var newRoot = root.ReplaceNode(oldType, newType);
            return newRoot;
        }

        private static Dictionary<string, string> ExtractMembersFromClass(SyntaxList<MemberDeclarationSyntax> typeMembers)
        {
            var members = new Dictionary<string, string>();
            foreach (var m in typeMembers)
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