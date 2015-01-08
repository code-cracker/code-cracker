Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<ExportCodeFixProviderAttribute("EmptyCatchBlockCodeFixProvider", LanguageNames.VisualBasic)>
Public Class EmptyCatchBlockCodeFixProvider
    Inherits CodeFixProvider

    Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
        Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
        Dim diag = context.Diagnostics.First
        Dim diagSpan = diag.Location.SourceSpan
        Dim declaration = root.FindToken(diagSpan.Start).Parent.AncestorsAndSelf.OfType(Of CatchBlockSyntax).First
        context.RegisterFix(CodeAction.Create("Remove Empty Catch Block", Function(c) RemoveEmptyCatchBlockAsync(context.Document, declaration, c)), diag)
        context.RegisterFix(CodeAction.Create("Remove Empty Catch Block and put a documentation link about Try...Catch usage", Function(c) RemoveEmptyCatchBlockPutCommentAsnyc(context.Document, declaration, c)), diag)
        context.RegisterFix(CodeAction.Create("Insert Exception class to Catch", Function(c) InsertExceptionClassCommentAsync(context.Document, declaration, c)), diag)
    End Function

    Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
        Return ImmutableArray.Create(EmptyCatchBlockAnalyzer.DiagnosticId)
    End Function

    Public Overrides Function GetFixAllProvider() As FixAllProvider
        Return WellKnownFixAllProviders.BatchFixer
    End Function

    Private Async Function RemoveTry(document As Document, catchBlock As CatchBlockSyntax, Optional insertComment As Boolean = False) As Task(Of Document)
        Dim tryBlock = DirectCast(catchBlock.Parent, TryBlockSyntax)
        Dim statements = tryBlock.Statements

        If insertComment Then
            Dim firstStatement = statements.FirstOrDefault()
            If firstStatement IsNot Nothing Then
                Dim comment() As SyntaxTrivia = {
                 SyntaxFactory.CommentTrivia("//TODO: Consider reading MSDN Documentation about how to use Try...Catch => http://msdn.microsoft.com/en-us/library/0yd65esw.aspx")
                    }

                firstStatement = firstStatement.WithLeadingTrivia(firstStatement.GetLeadingTrivia()).
                    WithTrailingTrivia(comment).
                    WithAdditionalAnnotations(Formatter.Annotation)

                'firstStatement = firstStatement.WithLeadingTrivia(SyntaxFactory.TriviaList(New SyntaxTrivia() {tryBlock.GetTrailingTrivia().First,
                '    SyntaxFactory.CommentTrivia("TODO: Consider reading MSDN Documentation about how to use Try...Catch => http://msdn.microsoft.com/en-us/library/0yd65esw.aspx"), tryBlock.GetTrailingTrivia().Last()})).
                '    WithAdditionalAnnotations(Formatter.Annotation)

                statements.Replace(statements.First(), firstStatement)
            End If
        End If

        Dim root = Await document.GetSyntaxRootAsync()
        Dim newRoot = root.ReplaceNode(catchBlock.Parent, statements.Select(Function(s) s.NormalizeWhitespace().
            WithLeadingTrivia(catchBlock.Parent.GetLeadingTrivia()).
            WithTrailingTrivia(catchBlock.Parent.GetTrailingTrivia())))
        Dim newDocument = document.WithSyntaxRoot(newRoot)
        Return newDocument
    End Function

    Private Async Function RemoveEmptyCatchBlockAsync(document As Document, catchBlock As CatchBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
        Return Await RemoveTry(document, catchBlock)
    End Function
    Private Async Function RemoveEmptyCatchBlockPutCommentAsnyc(document As Document, catchBlock As CatchBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
        Return Await RemoveTry(document, catchBlock, True)
    End Function
    Private Async Function InsertExceptionClassCommentAsync(document As Document, catchBlock As CatchBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
        Dim statements = New SyntaxList(Of SyntaxNode)().Add(SyntaxFactory.ThrowStatement())

        Dim catchStatement = SyntaxFactory.CatchStatement(
            SyntaxFactory.IdentifierName("ex"),
            SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName("Exception")),
            Nothing)

        Dim catchClause = SyntaxFactory.CatchBlock(catchStatement, statements).
            WithLeadingTrivia(catchBlock.GetLeadingTrivia).
            WithTrailingTrivia(catchBlock.GetTrailingTrivia).
            WithAdditionalAnnotations(Formatter.Annotation)

        Dim root = Await document.GetSyntaxRootAsync()
        Dim newRoot = root.ReplaceNode(catchBlock, catchClause)
        Dim newDocument = document.WithSyntaxRoot(newRoot)
        Return newDocument

    End Function

End Class
