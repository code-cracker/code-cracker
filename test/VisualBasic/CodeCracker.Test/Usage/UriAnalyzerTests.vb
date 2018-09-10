Imports CodeCracker.VisualBasic.Usage
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Usage
    Public Class UriAnalyzerTests
        Inherits CodeFixVerifier
        Private Const TestCode As String = "
Imports System
Namespace ConsoleApplication1
	Class Person
		Public Sub New()
            {0}
		End Sub
	End Class
End Namespace
"

#Disable Warning CC0063
        <Fact>
        Public Async Function IfAbbreviatedUriConstructorFoundAndUriIsIncorrectCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Dim uri = New Uri(""foo"")")
            Await VerifyBasicDiagnosticAsync(test, CreateDiagnosticResult(6, 31, Function() New Uri("foo")))
        End Function

        <Fact>
        Public Async Function IfUriConstructorFoundAndUriIsIncorrectCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Dim uri = New System.Uri(""foo"")")
            Await VerifyBasicDiagnosticAsync(test, CreateDiagnosticResult(6, 38, Function() New Uri("foo")))
        End Function

        <Fact>
        Public Async Function IfUriConstructorWithUriKindFoundAndUriIsIncorrectCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Dim uri = New Uri(""http://wrong"", UriKind.Relative)")
            Await VerifyBasicDiagnosticAsync(test, CreateDiagnosticResult(6, 31, Function() New Uri("http://wrong", UriKind.Relative)))
        End Function
#Enable Warning CC0063

        <Fact>
        Public Async Function IfAbbreviatedUriConstructorWithUriKindFoundAndUriIsCorrectDoNotCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Dim uri = New Uri(""foo"", UriKind.Relative)")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IfUriConstructorWithUriKindFoundAndUriIsCorrectDoNotCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Dim uri = New System.Uri(""foo"", UriKind.Relative)")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IfUriConstructorUsingNullFoundDoNotCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Dim uri = New System.Uri(null)")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IfUriConstructorNotUsingLiteralFoundDoNotCreatesDiagnostic() As Task
            Dim test = String.Format(TestCode, "Dim uri = New System.Uri(new object().ToString())")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IfAbbreviatedUriConstructorFoundAndUriIsIncorrectAndItsNotSystemUriDoNotCreatesDiagnostic() As Task
            Const code As String = "
Namespace ConsoleApplication1
	Class Uri
		Public Sub New(uri__1 As String)
		End Sub

		Public Sub Test()
			Dim uri = New Uri(""whoCares"")
		End Sub
	End Class
End Namespace"
            Await VerifyBasicHasNoDiagnosticsAsync(code)
        End Function

        Private Shared Function CreateDiagnosticResult(line As Integer, column As Integer, getErrorMessageAction As Action) As DiagnosticResult
            Return New DiagnosticResult(DiagnosticId.Uri.ToDiagnosticId(), DiagnosticSeverity.Error) _
                .WithLocation(line, column) _
                .WithMessage(GetErrorMessage(getErrorMessageAction))
        End Function

        Private Shared Function GetErrorMessage(action As Action) As String
            Try
                action.Invoke()
            Catch ex As Exception
                Return ex.Message
            End Try
            Return ""
        End Function

        Protected Overrides Function GetDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New UriAnalyzer()
        End Function
    End Class
End Namespace