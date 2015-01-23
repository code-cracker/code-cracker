Namespace TestHelper
    Partial Public Class DiagnosticVerifier
        Protected Async Function VerifyBasicHasNoDiagnosticsAsync(source As String) As Task
            Await VerifyBasicDiagnosticsAsync(source)
        End Function
    End Class
End Namespace
