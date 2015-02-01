Imports CodeCracker.Extensions
Imports Xunit

Namespace Extensions
    Public Class CodeCrackerExtensionTests
        <Fact>
        Public Sub CanFormatDiagnosticIdAsEnum()
            Dim id = DiagnosticId.ArgumentException
            Assert.Equal("CC0002", id.ToDiagnosticId())
        End Sub
    End Class
End Namespace
