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

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First
            context.RegisterCodeFix(CodeAction.Create("Remove this exception", Function(ct) RemoveThrow(context.Document, diagnostic, ct), NameOf(StaticConstructorExceptionCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Private Async Function RemoveThrow(document As Document, diagnostic As Diagnostic, cancellationToken As CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim sourceSpan = diagnostic.Location.SourceSpan
            Dim throwBlock = root.FindToken(sourceSpan.Start).Parent.AncestorsAndSelf.OfType(Of ThrowStatementSyntax).First

            Return document.WithSyntaxRoot((Await document.GetSyntaxRootAsync(cancellationToken)).RemoveNode(throwBlock, SyntaxRemoveOptions.KeepNoTrivia))
        End Function
    End Class
End Namespace