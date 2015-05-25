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

namespace CodeCracker.CSharp.Style
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ForInArrayCodeFixProvider)), Shared]
    public class ForInArrayCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> FixableDiagnosticIds =>
            ImmutableArray.Create(DiagnosticId.ForInArray.ToDiagnosticId());

        public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ForStatementSyntax>().First();
            context.RegisterCodeFix(CodeAction.Create("Change to foreach", c => MakeForeachAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> MakeForeachAsync(Document document, ForStatementSyntax forStatement, CancellationToken cancellationToken)
        {
            var semanticModel = await document.GetSemanticModelAsync(cancellationToken);
            var forBlock = forStatement.Statement as BlockSyntax;
            var condition = forStatement.Condition as BinaryExpressionSyntax;
            var arrayAccessor = condition.Right as MemberAccessExpressionSyntax;
            var arrayId = semanticModel.GetSymbolInfo(arrayAccessor.Expression).Symbol as ILocalSymbol;
            var controlVarId = semanticModel.GetDeclaredSymbol(forStatement.Declaration.Variables.Single());
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
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(forStatement, forEachStatement);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}