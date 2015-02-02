Module CodeCrackerExtensions
    <Extension>
    Public Function ToDiagnosticId(diagnosticId As DiagnosticId) As String
        Return "CC" & CInt(diagnosticId).ToString("D4")
    End Function
End Module