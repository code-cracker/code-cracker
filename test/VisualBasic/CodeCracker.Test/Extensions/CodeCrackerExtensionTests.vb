Imports CodeCracker.Extensions
Imports Xunit

Public Class CodeCrackerExtensionTests
    <Fact>
    Public Sub CodeCrackerExtensionCanFormatDiagnosticIdAsEnum()
        Dim id = DiagnosticId.ArgumentException
        Assert.Equal("CC0002", id.ToDiagnosticId())
    End Sub
End Class
