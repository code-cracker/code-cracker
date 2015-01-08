Imports CodeCracker.Test.TestHelper
Imports Xunit

Public Class StaticConstructorExceptionTests
    Inherits CodeFixTest(Of StaticConstructorExceptionAnalyzer, StaticConstructorExceptionCodeFixProvider)

    <Fact>
    Public Async Function WarningIfExceptionIsThrownInsideStaticConstructor() As Task
        Dim test = "
Public Class TestClass
    Shared Sub New()
        Throw New System.Exception()
    End Sub
End Class"

        Dim expected = New DiagnosticResult With {
            .Id = StaticConstructorExceptionAnalyzer.DiagnosticId,
            .Message = "Don't throw exceptions inside static constructors.",
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 4, 9)}
        }
        Await VerifyBasicDiagnosticsAsync(test, expected)
    End Function

    <Fact>
    Public Async Function NotWarningWhenNoExceptionIsThrownInsideStaticConstructor() As Task
        Dim test = "
Public Class TestClass
    Public Sub New()
        Throw New System.Exception()
    End Sub
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(test)
    End Function

    <Fact>
    Public Async Function StaticConstructorWithoutException() As Task
        Dim test = "
Public Class TestClass
    Shared Sub New()
        
    End Sub
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(test)

    End Function

    <Fact>
    Public Async Function InstanceConstructorWithoutException() As Task
        Dim test = "
Public Class TestClass
    Public Sub New()
        
    End Sub
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(test)

    End Function

    <Fact>
    Public Sub WhenThrowIsRemovedFromStaticConstructor()
        Dim test = "
Public Class TestClass
    Shared Sub New()
        Throw New System.Exception()
    End Sub
End Class"

        Dim fix = "
Public Class TestClass
    Shared Sub New()
    End Sub
End Class"
        VerifyBasicFix(test, fix, 0)

    End Sub

    Public Class TestClass
        Shared Sub New()
            Throw New System.Exception()
        End Sub
    End Class

End Class

