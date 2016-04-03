using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using Microsoft.CodeAnalysis.Simplification;
using System;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Design
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(UseInvokeMethodToFireEventCodeFixProvider)), Shared]
    public class UseInvokeMethodToFireEventCodeFixProvider : CodeFixProvider
    {
        private const string SyntaxAnnotatinKind = "CC-CopyEvent";

        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.UseInvokeMethodToFireEvent.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public async sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            var compilation = (CSharpCompilation)await context.Document.Project.GetCompilationAsync();
            if (compilation.LanguageVersion >= LanguageVersion.CSharp6)
                context.RegisterCodeFix(
                    CodeAction.Create("Change to ?.Invoke to call a delegate",
                    ct => UseInvokeAsync(context.Document, diagnostic, ct),
                    nameof(UseInvokeMethodToFireEventCodeFixProvider) + "_Elvis"), diagnostic);
            context.RegisterCodeFix(
                CodeAction.Create("Copy delegate reference to a variable",
                ct => CreateVariableAsync(context.Document, diagnostic, ct),
                nameof(UseInvokeMethodToFireEventCodeFixProvider) + "_CopyToVariable"), diagnostic);
        }

        private async static Task<Document> UseInvokeAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var sourceSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var newInvocation =
                    SyntaxFactory.ConditionalAccessExpression(
                        invocation.Expression,
                        SyntaxFactory.Token(SyntaxKind.QuestionToken),
                        SyntaxFactory.InvocationExpression(
                            SyntaxFactory.MemberBindingExpression(
                                SyntaxFactory.Token(SyntaxKind.DotToken),
                                SyntaxFactory.IdentifierName("Invoke")),
                                invocation.ArgumentList))
                    .WithAdditionalAnnotations(Formatter.Annotation)
                    .WithTrailingTrivia(invocation.GetTrailingTrivia());
            var identifier = (IdentifierNameSyntax)invocation.Expression;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var typeInfo = semanticModel.GetTypeInfo(identifier, cancellationToken);
            var symbol = semanticModel.GetSymbolInfo(identifier).Symbol;
            var invokedMethodSymbol = ((INamedTypeSymbol)typeInfo.ConvertedType).DelegateInvokeMethod;
            ExpressionSyntax newNode = newInvocation;
            if (!invokedMethodSymbol.ReturnsVoid && !invokedMethodSymbol.ReturnType.IsReferenceType && !(invocation.Parent is ExpressionStatementSyntax))
            {
                var typeName = invokedMethodSymbol.ReturnType.GetFullName(false);
                var defaultValue = SyntaxFactory.DefaultExpression(SyntaxFactory.ParseName(typeName)
                    .WithAdditionalAnnotations(Simplifier.Annotation));
                newNode = SyntaxFactory.BinaryExpression(SyntaxKind.CoalesceExpression, newInvocation, defaultValue)
                    .WithAdditionalAnnotations(Formatter.Annotation);
                if (invocation.Parent is BinaryExpressionSyntax)
                {
                    var binary = (BinaryExpressionSyntax)invocation.Parent;
                    if (binary.Right.Equals(invocation.Parent)
                        && (binary.IsKind(SyntaxKind.BitwiseAndExpression)
                        || binary.IsKind(SyntaxKind.ExclusiveOrExpression)
                        || binary.IsKind(SyntaxKind.BitwiseOrExpression)
                        || binary.IsKind(SyntaxKind.LogicalAndExpression))
                        || binary.IsKind(SyntaxKind.LogicalOrExpression)
                        || binary.IsKind(SyntaxKind.CoalesceExpression))
                        newNode = SyntaxFactory.ParenthesizedExpression(newNode);
                }
            }
            var newRoot = root.ReplaceNode(invocation, newNode);
            return document.WithSyntaxRoot(newRoot);
        }

        private async static Task<Document> CreateVariableAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var sourceSpan = diagnostic.Location.SourceSpan;
            var invocation = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf().OfType<InvocationExpressionSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var handlerName = semanticModel.FindAvailableIdentifierName(sourceSpan.Start, "handler");
            var variable =
                    SyntaxFactory.LocalDeclarationStatement(
                        SyntaxFactory.VariableDeclaration(
                            SyntaxFactory.ParseTypeName("var"),
                            SyntaxFactory.SeparatedList(
                                new[]
                                {
                                    SyntaxFactory.VariableDeclarator(
                                        SyntaxFactory.Identifier(handlerName),
                                        null,
                                        SyntaxFactory.EqualsValueClause(invocation.Expression.WithoutLeadingTrivia().WithoutTrailingTrivia()))
                                })))
                    .WithLeadingTrivia(invocation.Parent.GetLeadingTrivia());
            var statement = invocation.Expression.FirstAncestorOrSelfThatIsAStatement();
            var newStatement = statement.ReplaceNode(invocation.Expression, SyntaxFactory.IdentifierName(handlerName));
            var newInvocation =
                    SyntaxFactory.IfStatement(
                        SyntaxFactory.BinaryExpression(SyntaxKind.NotEqualsExpression,
                            SyntaxFactory.IdentifierName(handlerName),
                            SyntaxFactory.LiteralExpression(SyntaxKind.NullLiteralExpression)),
                        newStatement)
                    .WithTrailingTrivia(invocation.Parent.GetTrailingTrivia());
            var oldNode = statement;
            var newNode = newStatement.WithAdditionalAnnotations(new SyntaxAnnotation(SyntaxAnnotatinKind));
            if (oldNode.Parent.IsEmbeddedStatementOwner())
                newNode = SyntaxFactory.Block((StatementSyntax)newNode);
            var newRoot = root.ReplaceNode(oldNode, newNode);
            newRoot = newRoot.InsertNodesAfter(GetMark(newRoot), new SyntaxNode[] { variable, newInvocation });
            newRoot = newRoot.RemoveNode(GetMark(newRoot), SyntaxRemoveOptions.KeepNoTrivia);
            return document.WithSyntaxRoot(newRoot.WithAdditionalAnnotations(Formatter.Annotation));
        }

        private static SyntaxNode GetMark(SyntaxNode node) =>
            node.DescendantNodes().First(n => n.GetAnnotations(SyntaxAnnotatinKind).Any());
    }
}