Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Formatting
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports System.Threading

Namespace Design
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(EmptyCatchBlockCodeFixProvider)), Composition.Shared>
    Public Class EmptyCatchBlockCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.EmptyCatchBlock.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diag = context.Diagnostics.First
            Dim diagSpan = diag.Location.SourceSpan
            Dim declaration = root.FindToken(diagSpan.Start).Parent.AncestorsAndSelf.OfType(Of CatchBlockSyntax).First
            context.RegisterCodeFix(CodeAction.Create("Remove Empty Catch Block", Function(c) RemoveTry(context.Document, declaration, c), NameOf(EmptyCatchBlockCodeFixProvider) & NameOf(RemoveTry)), diag)
            context.RegisterCodeFix(CodeAction.Create("Insert Exception class to Catch", Function(c) InsertExceptionClassCommentAsync(context.Document, declaration, c), NameOf(EmptyCatchBlockCodeFixProvider) & NameOf(InsertExceptionClassCommentAsync)), diag)
        End Function

        Private Async Function RemoveTry(document As Document, catchBlock As CatchBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim tryBlock = DirectCast(catchBlock.Parent, TryBlockSyntax)
            Dim statements = tryBlock.Statements
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)

            Dim newRoot = root.ReplaceNode(catchBlock.Parent,
                                       statements.Select(Function(s) s.
                                            WithLeadingTrivia(catchBlock.Parent.GetLeadingTrivia()).
                                            WithTrailingTrivia(catchBlock.Parent.GetTrailingTrivia())))

            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument
        End Function

        Private Async Function InsertExceptionClassCommentAsync(document As Document, catchBlock As CatchBlockSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Dim statements = New SyntaxList(Of SyntaxNode)().Add(SyntaxFactory.ThrowStatement())

            Dim catchStatement = SyntaxFactory.CatchStatement(
            SyntaxFactory.IdentifierName("ex"),
            SyntaxFactory.SimpleAsClause(SyntaxFactory.IdentifierName(NameOf(Exception))),
            Nothing)

            Dim catchClause = SyntaxFactory.CatchBlock(catchStatement, statements).
            WithLeadingTrivia(catchBlock.GetLeadingTrivia).
            WithTrailingTrivia(catchBlock.GetTrailingTrivia).
            WithAdditionalAnnotations(Formatter.Annotation)

            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim newRoot = root.ReplaceNode(catchBlock, catchClause)
            Dim newDocument = document.WithSyntaxRoot(newRoot)
            Return newDocument

        End Function

    End Class
End Namespace