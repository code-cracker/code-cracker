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

namespace CodeCracker.Style
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
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ConstructorDeclarationSyntax>().First();
            context.RegisterFix(CodeAction.Create("Introduce field from constructor.", c => IntroduceFieldFromConstructorAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> IntroduceFieldFromConstructorAsync(Document document, ConstructorDeclarationSyntax constructorStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);

            var methodMembers = (constructorStatement.Parent as ClassDeclarationSyntax).Members;
            var fieldMembers = methodMembers.OfType<FieldDeclarationSyntax>();
            var parameters = constructorStatement.ParameterList.Parameters;

            var root = await document.GetSyntaxRootAsync();
            var newDocument = document;
            foreach (var par in parameters)
            {
                var parName = par.Identifier.Text;
                var fieldName = parName;
                var field = fieldMembers.FirstOrDefault(p => p.Declaration.Variables.First().Identifier == par.Identifier && p.Declaration.Type == par.Type);
                if (field == null)
                {
                    //var memberField = SyntaxFactory.FieldDeclaration(SyntaxFactory.VariableDeclaration(SyntaxFactory.PredefinedType(SyntaxFactory.Token(SyntaxKind.IntKeyword)))
                    //                  .WithVariables(SyntaxFactory.SingletonSeparatedList<VariableDeclaratorSyntax>(SyntaxFactory.VariableDeclarator(SyntaxFactory.Identifier(parName)))))
                    //                  .WithModifiers(SyntaxFactory.TokenList(new[] { SyntaxFactory.Token(SyntaxKind.PrivateKeyword), SyntaxFactory.Token(SyntaxKind.ReadOnlyKeyword) }))
                    //                  .WithAdditionalAnnotations(Formatter.Annotation);

                    //var newRootField = root.InsertNodesAfter(fieldMembers.Last(), new[] { memberField as SyntaxNode });
                    //newDocument = newDocument.WithSyntaxRoot(newRootField);

                    //root = await newDocument.GetSyntaxRootAsync();

                    var assignField = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
                                               SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(),
                                               SyntaxFactory.IdentifierName(fieldName)), SyntaxFactory.IdentifierName(parName));

                    var assignStatement = SyntaxFactory.ExpressionStatement(assignField)
                                    .WithSemicolonToken(SyntaxFactory.Token(SyntaxKind.SemicolonToken))
                                    .WithAdditionalAnnotations(Formatter.Annotation)
                                    .NormalizeWhitespace();

                    SyntaxNode newRootAssign;
                    if (constructorStatement.Body.Statements.Count == 0)
                    {
                        var newBody = constructorStatement.WithBody(constructorStatement.Body.AddStatements(assignStatement));
                        newRootAssign = root.ReplaceNode(constructorStatement, newBody);
                    }
                    else
                    {
                        newRootAssign = root.InsertNodesAfter(constructorStatement.Body.Statements.Last(), new[] { assignStatement as SyntaxNode });
                    }
                    newDocument = newDocument.WithSyntaxRoot(newRootAssign);




                    return newDocument;
                }
            //    var assignField = SyntaxFactory.AssignmentExpression(SyntaxKind.SimpleAssignmentExpression,
            //                                    SyntaxFactory.MemberAccessExpression(SyntaxKind.SimpleMemberAccessExpression, SyntaxFactory.ThisExpression(),
            //                                    SyntaxFactory.IdentifierName(fieldName)), SyntaxFactory.IdentifierName(parName));

            //    var assignStatement = SyntaxFactory.ExpressionStatement(assignField)
            //                    .WithSemicolonToken(SyntaxFactory.MissingToken(SyntaxKind.SemicolonToken))
            //                    .WithAdditionalAnnotations(Formatter.Annotation);
            //    //root = await newDocument.GetSyntaxRootAsync();
            //    SyntaxNode newRootAssign;
            //    if (constructorStatement.Body.Statements.Count == 0)
            //    {
            //        var newBody = constructorStatement.Body.Statements.Insert(0, assignStatement);
            //        newRootAssign = root.ReplaceNode(constructorStatement.Body, newBody);
            //    }
            //    else
            //    {
            //        newRootAssign = root.InsertNodesAfter(constructorStatement.Body.Statements.Last(), new[] { assignStatement as SyntaxNode });
            //    }
            //    newDocument = newDocument.WithSyntaxRoot(newRootAssign);

            }

            return newDocument;
        }
    }
}