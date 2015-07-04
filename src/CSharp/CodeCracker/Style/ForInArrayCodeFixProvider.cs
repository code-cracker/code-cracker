using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Formatting;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ForInArrayCodeFixProvider)), Shared]
    public class ForInArrayCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ForInArray.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var diagnostic = context.Diagnostics.First();
            context.RegisterCodeFix(CodeAction.Create("Change to foreach", c => MakeForeachAsync(context.Document, diagnostic, c)), diagnostic);
            return Task.FromResult(0);
        }

        private async static Task<Document> MakeForeachAsync(Document document, Diagnostic diagnostic, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var forStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ForStatementSyntax>().First();
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var forBlock = (BlockSyntax)forStatement.Statement;
            var condition = (BinaryExpressionSyntax)forStatement.Condition;
            var arrayAccessor = (MemberAccessExpressionSyntax)condition.Right;
            var arrayId = semanticModel.GetSymbolInfo(arrayAccessor.Expression).Symbol as ILocalSymbol;
            var forVariable = forStatement.Declaration.Variables.Single();
            var controlVarId = semanticModel.GetDeclaredSymbol(forVariable);
            var arrayDeclarations = (from s in forBlock.Statements.OfType<LocalDeclarationStatementSyntax>()
                                     where s.Declaration.Variables.Count == 1
                                     let declaration = s.Declaration.Variables.First()
                                     where declaration?.Initializer?.Value is ElementAccessExpressionSyntax
                                     let init = (ElementAccessExpressionSyntax)declaration.Initializer.Value
                                     let initSymbol = semanticModel.GetSymbolInfo(init.ArgumentList.Arguments.First().Expression).Symbol
                                     where controlVarId.Equals(initSymbol)
                                     let someArrayInit = semanticModel.GetSymbolInfo(init.Expression).Symbol as ILocalSymbol
                                     where someArrayInit == null || someArrayInit.Equals(arrayId)
                                     select s).ToList();
            var arrayDeclaration = arrayDeclarations.First();
            var blockForFor = forBlock.RemoveNode(arrayDeclaration, SyntaxRemoveOptions.KeepLeadingTrivia);

            var forEachStatement = SyntaxFactory.ForEachStatement(SyntaxFactory.ParseTypeName("var"), arrayDeclaration.Declaration.Variables.First().Identifier, arrayAccessor.Expression, blockForFor)
                .WithLeadingTrivia(forStatement.GetLeadingTrivia())
                .WithTrailingTrivia(forStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);

            var foreachVariable = SyntaxFactory.IdentifierName(forEachStatement.Identifier.Text);
            var elementsAccessorsToReplace = forEachStatement.DescendantNodes()
                            .OfType<ElementAccessExpressionSyntax>()
                            .Where(eae => eae.ArgumentList.Arguments.Any(a => a.Expression.ToFullString().Trim() == forVariable.Identifier.ToFullString().Trim()))
                            .ToList();

            forEachStatement = forEachStatement.ReplaceNodes(elementsAccessorsToReplace, (original, rewritten) => foreachVariable);

            var newRoot = root.ReplaceNode(forStatement, forEachStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}