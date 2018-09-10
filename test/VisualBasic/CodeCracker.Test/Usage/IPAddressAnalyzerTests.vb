Imports System.Net
Imports CodeCracker.VisualBasic
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Public Class IPAddressAnalyzerTests
    Inherits CodeFixVerifier

    Private Const TestCode = "
Imports System
Imports System.Net
Namespace ConsleApplication1
    Class Person
        Public Sub New()
            {0}
        End Sub
    End Class
End Namespace"

    <Fact>
    Public Async Function IfParseIdentifierFoundAndIpAddressTextIsIncorrectCreatesDiagnostic() As Task
        Dim test = String.Format(TestCode, "System.Net.IPAddress.Parse(""foo"")")
#Disable Warning CC0064
        Await VerifyBasicDiagnosticAsync(test, CreateDiagnosticResult(7, 40, Sub() IPAddress.Parse("foo")))
#Enable Warning CC0064
    End Function

    <Fact>
    Public Async Function IfAbbreviatedParseIdentifierFoundAndIPAddressTextIsIncorrectCreatesDiagnostic() As Task
        Dim test = String.Format(TestCode, "IPAddress.Parse(""foo"")")
#Disable Warning CC0064
        Await VerifyBasicDiagnosticAsync(test, CreateDiagnosticResult(7, 29, Sub() IPAddress.Parse("foo")))
#Enable Warning CC0064
    End Function

    <Fact>
    Public Async Function IfParseIdentifierFoundAndIPAddressTextIsCorrectDoesNotCreateDiagnostic() As Task
        Dim test = String.Format(TestCode, "System.Net.IPAddress.Parse(""127.0.0.1"")")
        Await VerifyBasicHasNoDiagnosticsAsync(test)
    End Function

    <Fact>
    Public Async Function IfAbbreviatedParseIdentifierFoundAndIPAddressTextIsCorrectDoesNotCreateDiagnostic() As Task
        Dim test = String.Format(TestCode, "IPAddress.Parse(""127.0.0.1"")")
        Await VerifyBasicHasNoDiagnosticsAsync(test)
    End Function

    <Fact>
    Public Async Function IfIsOtherTypeParseMethodDoesNotCreateDiagnostic() As Task
        Dim test = String.Format(TestCode, "[Enum].Parse(GetType(String), """")")
        Await VerifyBasicHasNoDiagnosticsAsync(test)
    End Function

    Private Function CreateDiagnosticResult(line As Integer, column As Integer, errorMessageAction As Action) As DiagnosticResult
        Return New DiagnosticResult(DiagnosticId.IPAddress.ToDiagnosticId(), DiagnosticSeverity.Error) _
            .WithLocation(line, column) _
            .WithMessage(GetErrorMessage(errorMessageAction))
    End Function

    Private Shared Function GetErrorMessage(action As Action) As String
        Try
            action()
        Catch ex As Exception
            Return ex.Message
        End Try
        Return String.Empty
    End Function

    Protected Overrides Function GetDiagnosticAnalyzer() As DiagnosticAnalyzer
        Return New IPAddressAnalyzer
    End Function
End Class

