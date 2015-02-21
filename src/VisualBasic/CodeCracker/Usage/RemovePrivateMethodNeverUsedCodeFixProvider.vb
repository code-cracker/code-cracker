Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.CodeActions
Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

Namespace Usage
    <ExportCodeFixProvider("CodeCrackerRemovePrivateMethodNeverUsedCodeFixProvider", LanguageNames.VisualBasic)>
    <Composition.Shared>
    Public Class RemovePrivateMethodNeverUsedCodeFixProvider
        Inherits CodeFixProvider

        Public Overrides Function GetFixableDiagnosticIds() As ImmutableArray(Of String)
            Return ImmutableArray.Create(DiagnosticId.RemovePrivateMethodNeverUsed.ToDiagnosticId())
        End Function

        Public Overrides Function GetFixAllProvider() As FixAllProvider
            Return WellKnownFixAllProviders.BatchFixer
        End Function

        Public Overrides Async Function ComputeFixesAsync(context As CodeFixContext) As Task
            Dim root = Await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(False)
            Dim diagnostic = context.Diagnostics.First()
            Dim span = diagnostic.Location.SourceSpan
            Dim methodNotUsed = root.FindToken(span.Start).Parent.FirstAncestorOrSelf(Of MethodStatementSyntax)
            context.RegisterFix(CodeAction.Create("Remove unused private method: " & methodNotUsed.Identifier.ValueText, Function(c) RemoveMethodAsync(context.Document, methodNotUsed, c)), diagnostic)
        End Function

        Private Async Function RemoveMethodAsync(document As Document, methodNotUsed As MethodStatementSyntax, cancellationToken As Threading.CancellationToken) As Task(Of Document)
            Dim root = Await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(False)
            Dim newRoot = root.RemoveNode(methodNotUsed.Parent, SyntaxRemoveOptions.KeepNoTrivia)
            Return document.WithSyntaxRoot(newRoot)
        End Function
    End Class
End Namespace