Partial Public Class DiagnosticVerifier
    Protected Async Function VerifyBasicHasNoDiagnosticsAsync(source As String) As Task
        Await VerifyDiagnosticsAsync(source)
    End Function
End Class