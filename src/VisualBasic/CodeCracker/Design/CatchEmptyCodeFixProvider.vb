Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Threading

Namespace Design
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(CatchEmptyCodeFixProvider)), Composition.Shared>
    Public Class CatchEmptyCodeFixProvider
        Inherits CodeFixProvider

        Public NotOverridable Overrides ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(CatchEmptyAnalyzer.Id)

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diag = context.Diagnostics.First()
            context.RegisterCodeFix(CodeAction.Create("Add an Exception class", Function(c) MakeCatchEmptyAsync(context.Document, diag, c), NameOf(CatchEmptyCodeFixProvider)), diag)
            Return Task.FromResult(0)
        End Function

        Private Async Function MakeCatchEmptyAsync(document As Document, diag As Diagnostic, cancellationtoken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationtoken).ConfigureAwait(False)
            Dim diagSpan = diag.Location.SourceSpan
            Dim catchStatement = root.FindToken(diagSpan.Start).Parent.AncestorsAndSelf.OfType(Of CatchBlockSyntax).First()
            Dim semanticModel = Await document.GetSemanticModelAsync(cancellationtoken)

            Dim newCatch = SyntaxFactory.CatchBlock(
            SyntaxFactory.CatchStatement(
                SyntaxFactory.IdentifierName("ex"),
                SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(NameOf(Exception))),
                Nothing)).
                WithStatements(catchStatement.Statements).
                WithLeadingTrivia(catchStatement.GetLeadingTrivia).
                WithTrailingTrivia(catchStatement.GetTrailingTrivia).
                WithAdditionalAnnotations(Formatter.Annotation)

            Dim newRoot = root.ReplaceNode(catchStatement, newCatch)
            Dim newDoc = document.WithSyntaxRoot(newRoot)
            Return newDoc
        End Function
    End Class
End Namespace