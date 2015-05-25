using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
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
    [ExportCodeFixProvider(nameof(StringBuilderInLoopCodeFixProvider), LanguageNames.CSharp), Shared]
    public class StringBuilderInLoopCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.StringBuilderInLoop.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => null; //todo: allow for a fixall but only if we can fix the clash on the builder name in a nice way

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var assignmentExpression = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<AssignmentExpressionSyntax>().First();
            context.RegisterCodeFix(CodeAction.Create($"Use StringBuilder to create a value for '{assignmentExpression.Left.ToString()}'", c => UseStringBuilderAsync(context.Document, assignmentExpression, c)), diagnostic);
        }

        private async Task<Document> UseStringBuilderAsync(Document document, AssignmentExpressionSyntax assignmentExpression, CancellationToken cancellationToken)
        {
            var expressionStatement = assignmentExpression.Parent;
            var expressionStatementParent = expressionStatement.Parent;
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var builderName = FindAvailableStringBuilderVariableName(assignmentExpression, semanticModel);
            var loopStatement = expressionStatementParent.FirstAncestorOrSelfOfType(typeof(WhileStatementSyntax),
                typeof(ForStatementSyntax),
                typeof(ForEachStatementSyntax),
                typeof(DoStatementSyntax));
            var newExpressionStatementParent = ReplaceAddExpressionByStringBuilderAppendExpression(assignmentExpression, expressionStatement, expressionStatementParent, builderName);
            var newLoopStatement = loopStatement.ReplaceNode(expressionStatementParent, newExpressionStatementParent);
            var stringBuilderType = SyntaxFactory.ParseTypeName("System.Text.StringBuilder").WithAdditionalAnnotations(Simplifier.Annotation);
            var stringBuilderDeclaration = SyntaxFactory.LocalDeclarationStatement(
                SyntaxFactory.VariableDeclaration(
                    SyntaxFactory.ParseTypeName("var"),
                    SyntaxFactory.SeparatedList(
                        new[] {
                            SyntaxFactory.VariableDeclarator(
                                SyntaxFactory.Identifier(builderName),
                                null,
                                SyntaxFactory.EqualsValueClause(
                                    SyntaxFactory.ObjectCreationExpression(stringBuilderType)
                                    .WithArgumentList(SyntaxFactory.ArgumentList()))
                        )})));
            var appendExpressionOnInitialization = SyntaxFactory.ParseStatement($"{builderName}.Append({assignmentExpression.Left.ToString()});\r\n");
            var stringBuilderToString = SyntaxFactory.ParseStatement($"{assignmentExpression.Left.ToString()} = {builderName}.ToString();\r\n");
            var loopParent = loopStatement.Parent;
            var newLoopParent = loopParent.ReplaceNode(loopStatement,
                new[] {
                    stringBuilderDeclaration,
                    appendExpressionOnInitialization,
                    newLoopStatement,
                    stringBuilderToString
                }).WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(loopParent, newLoopParent);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }

        private static SyntaxNode ReplaceAddExpressionByStringBuilderAppendExpression(AssignmentExpressionSyntax assignmentExpression, SyntaxNode expressionStatement, SyntaxNode expressionStatementParent, string builderName)
        {
            var appendExpressionOnLoop = assignmentExpression.IsKind(SyntaxKind.SimpleAssignmentExpression)
                ? SyntaxFactory.ParseStatement($"{builderName}.Append({((BinaryExpressionSyntax)assignmentExpression.Right).Right.ToString()});\r\n")
                : SyntaxFactory.ParseStatement($"{builderName}.Append({assignmentExpression.Right.ToString()});\r\n");
            appendExpressionOnLoop = appendExpressionOnLoop
                .WithLeadingTrivia(expressionStatement.GetLeadingTrivia())
                .WithTrailingTrivia(expressionStatement.GetTrailingTrivia());
            var newExpressionStatementParent = expressionStatementParent.ReplaceNode(expressionStatement, appendExpressionOnLoop);
            return newExpressionStatementParent;
        }

        private static string FindAvailableStringBuilderVariableName(AssignmentExpressionSyntax assignmentExpression, SemanticModel semanticModel)
        {
            const string builderNameBase = "builder";
            var builderName = builderNameBase;
            var builderNameIncrementer = 0;
            while (semanticModel.LookupSymbols(assignmentExpression.GetLocation().SourceSpan.Start, name: builderName).Any())
                builderName = builderNameBase + ++builderNameIncrementer;
            return builderName;
        }
    }
}