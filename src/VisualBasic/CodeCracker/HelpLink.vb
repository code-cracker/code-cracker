Public Module HelpLink
    Public Function ForDiagnostic(diagnosticId As String) As String
        Return String.Format("https://code-cracker.github.io/diagnostics/{0}.html", diagnosticId)
    End Function
End Module
