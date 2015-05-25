Imports System.Collections.Immutable
Imports System.Threading
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeActions
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Design
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(StaticConstructorExceptionCodeFixProvider)), Composition.Shared>
    Public Class StaticConstructorExceptionCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(StaticConstructorExceptionAnalyzer.Id)

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Async Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First
            Dim sourceSpan = diagnostic.Location.SourceSpan
            Dim throwBlock = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf.OfType(Of ThrowStatementSyntax).First
            context.RegisterCodeFix(CodeAction.Create("Remove this exception", Function(ct) RemoveThrow(context.Document, throwBlock, ct)), diagnostic)
        End Function

        Private Async Function RemoveThrow(document As Document, throwBlock As ThrowStatementSyntax, cancellationToken As CancellationToken) As Task(Of Document)
            Return document.WithSyntaxRoot((Await document.GetSyntaxRootAsync(cancellationToken)).RemoveNode(throwBlock, SyntaxRemoveOptions.KeepNoTrivia))
        End Function
    End Class
End Namespace