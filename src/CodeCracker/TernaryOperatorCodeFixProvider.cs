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

namespace CodeCracker
{
    [ExportCodeFixProvider("CodeCrackerTernaryOperatorCodeFixProvider", LanguageNames.CSharp), Shared]
    public class TernaryOperatorCodeFixProvider : CodeFixProvider
    {
        public sealed override ImmutableArray<string> GetFixableDiagnosticIds()
        {
            return ImmutableArray.Create(TernaryOperatorAnalyzer.DiagnosticId);
        }

        public sealed override FixAllProvider GetFixAllProvider()
        {
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task ComputeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var declaration = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<IfStatementSyntax>().First();
            context.RegisterFix(CodeAction.Create("Change to ternary operator", c => MakeTernaryAsync(context.Document, declaration, c)), diagnostic);
        }

        private async Task<Document> MakeTernaryAsync(Document document, IfStatementSyntax ifStatement, CancellationToken cancellationToken)
        {
            var statementInsideIf = (ReturnStatementSyntax)(ifStatement.Statement is BlockSyntax ? ((BlockSyntax)ifStatement.Statement).Statements.Single() : ifStatement.Statement);
            var elseStatement = ifStatement.Else;
            var statementInsideElse = (ReturnStatementSyntax)(elseStatement.Statement is BlockSyntax ? ((BlockSyntax)elseStatement.Statement).Statements.Single() : elseStatement.Statement);
            var ternary = SyntaxFactory.ParseStatement("return \{ifStatement.Condition.ToString()} ? \{statementInsideIf.Expression.ToString()} : \{statementInsideElse.Expression.ToString()};")
                .WithLeadingTrivia(ifStatement.GetLeadingTrivia())
                .WithTrailingTrivia(ifStatement.GetTrailingTrivia())
                .WithAdditionalAnnotations(Formatter.Annotation);
            var root = await document.GetSyntaxRootAsync();
            var newRoot = root.ReplaceNode(ifStatement, ternary);
            var newDocument = document.WithSyntaxRoot(newRoot);
            return newDocument;
        }
    }
}