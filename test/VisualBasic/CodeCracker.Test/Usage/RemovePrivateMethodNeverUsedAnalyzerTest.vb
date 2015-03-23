Imports CodeCracker.VisualBasic.Usage
Imports Xunit

Namespace Usage
    Public Class RemovePrivateMethodNeverUsedAnalyzerTest
        Inherits CodeFixVerifier(Of RemovePrivateMethodNeverUsedAnalyzer, RemovePrivateMethodNeverUsedCodeFixProvider)

		<Fact>
		Public Async Function DoesNotGenerateDiagnostics() As Task
			Const test = "
Public Class Foo
    Public Sub PublicFoo
        PrivateFoo()
    End Sub
    Private Sub PrivateFoo
        PrivateFoo2
    End Sub
    Private Sub PrivateFoo2
    End Sub
End Class"

			Await VerifyBasicHasNoDiagnosticsAsync(test)
		End Function

		<Fact>
		Public Async Function WhenPrivateMethodUsedInPartialClassesDoesNotGenerateDiagnostics() As Task
			Const test = "
Public Partial Class Foo
    Public Sub PublicFoo
        PrivateFoo()
    End Sub
End Class

Public Partial Class Foo
    Private Sub PrivateFoo
    End Sub
End Class"

			Await VerifyBasicHasNoDiagnosticsAsync(test)
		End Function

		<Fact>
		Public Async Function WhenPrivateMethodIsNotUsedInPartialClassesItShouldBeRemoved() As Task
			Const test = "
Public Partial Class Foo
    Public Sub PublicFoo
    End Sub
End Class

Public Partial Class Foo
    Private Sub PrivateFoo
    End Sub
End Class"

			Const fix = "
Public Partial Class Foo
    Public Sub PublicFoo
    End Sub
End Class

Public Partial Class Foo
End Class"

			Await VerifyBasicFixAsync(test, fix)
		End Function

		<Fact>
        Public Async Function WhenPrivateMethodUsedDoesNotGenerateDiagnostics() As Task
            Const test = "
Public Class Foo
    Public Sub PublicFoo
        PrivateFoo()
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function WhenPrivateMethodIsNotUsedShouldCreateDiagnostic() As Task
            Const test = "
Class Foo
    Private Sub PrivateFoo()
    End Sub
End Class"

            Const fix = "
Class Foo
End Class"
            Await VerifyBasicFixAsync(test, fix)
        End Function

        <Fact>
        Public Async Function WhenPrivateMethodUsedInAttributionDoesNotGenerateDiagnostics() As Task
            Const test = "
Imports System
Class Foo
    Public Sub PublicFoo()
        Dim method As Action = AddressOf PrivateFoo
    End Sub
    Private Sub PrivateFoo()
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function
    End Class
End Namespace