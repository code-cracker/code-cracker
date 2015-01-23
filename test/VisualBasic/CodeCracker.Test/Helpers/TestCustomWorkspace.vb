Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Text

Public Class TestCustomWorkspace
    Inherits Microsoft.CodeAnalysis.CustomWorkspace
    Protected Overrides Sub AddProjectReference(projectId As ProjectId, projectReference As ProjectReference)
        OnProjectReferenceAdded(projectId, projectReference)
    End Sub

    Protected Overrides Sub RemoveProjectReference(projectId As ProjectId, projectReference As ProjectReference)
        OnProjectReferenceRemoved(projectId, projectReference)
    End Sub

    Protected Overrides Sub AddMetadataReference(projectId As ProjectId, metadataReference As MetadataReference)
        OnMetadataReferenceAdded(projectId, metadataReference)
    End Sub

    Protected Overrides Sub RemoveMetadataReference(projectId As ProjectId, metadataReference As MetadataReference)
        OnMetadataReferenceRemoved(projectId, metadataReference)
    End Sub
    Protected Overrides Sub AddAnalyzerReference(projectId As ProjectId, analyzerReference As AnalyzerReference)
        OnAnalyzerReferenceAdded(projectId, analyzerReference)
    End Sub
    Protected Overrides Sub RemoveAnalyzerReference(projectId As ProjectId, analyzerReference As AnalyzerReference)
        OnAnalyzerReferenceRemoved(projectId, analyzerReference)
    End Sub
    'Protected Overrides Sub AddDocument(documentId As DocumentId, folders As IEnumerable(Of String), name As String, Optional text As SourceText = Nothing, Optional sourceCodeKind As SourceCodeKind = SourceCodeKind.Regular)
    '    Dim docInfo = DocumentInfo.Create(documentId, name, folders, sourceCodeKind,
    '                                        TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())))
    '    OnDocumentAdded(docInfo)
    'End Sub
    Protected Overrides Sub RemoveDocument(documentId As DocumentId)
        OnDocumentRemoved(documentId)
    End Sub
    Protected Overrides Sub ChangedDocumentText(id As DocumentId, text As SourceText)
        OnDocumentTextChanged(id, text, PreservationMode.PreserveValue)
    End Sub
    'Protected Overrides Sub AddAdditionalDocument(documentId As DocumentId, folders As IEnumerable(Of String), name As String, Optional text As SourceText = Nothing)
    '    Dim docInfo = DocumentInfo.Create(documentId, name, folders,
    '                                      loader:=TextLoader.From(TextAndVersion.Create(text, VersionStamp.Create())))
    '    OnAdditionalDocumentAdded(docInfo)
    'End Sub
    Protected Overrides Sub RemoveAdditionalDocument(documentId As DocumentId)
        OnAdditionalDocumentRemoved(documentId)
    End Sub
    Protected Overrides Sub ChangedAdditionalDocumentText(id As DocumentId, text As SourceText)
        OnAdditionalDocumentTextChanged(id, text, PreservationMode.PreserveValue)
    End Sub
End Class
