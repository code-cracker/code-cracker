Imports Microsoft.CodeAnalysis.CodeFixes
Imports Microsoft.CodeAnalysis.Diagnostics

Namespace TestHelper
    Public MustInherit Class CodeFixTest(Of T As {DiagnosticAnalyzer, New}, U As {CodeFixProvider, New})
        Inherits CodeFixVerifier

        Protected Overrides Function GetDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New T()
        End Function

        Protected Overrides Function GetBasicCodeFixProvider() As CodeFixProvider
            Return New U()
        End Function
    End Class
End Namespace