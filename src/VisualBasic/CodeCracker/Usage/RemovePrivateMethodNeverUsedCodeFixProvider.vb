Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <ExportCodeFixProvider(LanguageNames.VisualBasic, Name:=NameOf(RemovePrivateMethodNeverUsedCodeFixProvider)), Composition.Shared>
    Public Class RemovePrivateMethodNeverUsedCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides NotOverridable ReadOnly Property FixableDiagnosticIds As ImmutableArray(Of String) = ImmutableArray.Create(DiagnosticId.RemovePrivateMethodNeverUsed.ToDiagnosticId())

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Function RegisterCodeFixesAsync(context As CodeFixContext) As Task
            Dim diagnostic = context.Diagnostics.First()
            context.RegisterCodeFix(CodeAction.Create($"Remove unused private method: {diagnostic.Properties!identifier}", Function(c) RemoveMethodAsync(context.Document, diagnostic, c), NameOf(RemovePrivateMethodNeverUsedCodeFixProvider)), diagnostic)
            Return Task.FromResult(0)
        End Function

        Private Async Function RemoveMethodAsync(document As Document, diagnostic As Diagnostic, cancellationToken As Threading.CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim span = diagnostic.Location.SourceSpan
            Dim methodNotUsed = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of MethodStatementSyntax)

            Dim newRoot = root.RemoveNode(methodNotUsed.Parent, SyntaxRemoveOptions.KeepNoTrivia)
            Return document.WithSyntaxRoot(newRoot)
        End Function
    End Class
End Namespace