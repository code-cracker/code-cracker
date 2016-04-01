Imports CodeCracker.VisualBasic.Usage
Imports Xunit

Namespace Usage
    Public Class RemovePrivateMethodNeverUsedAnalyzerTest
        Inherits CodeFixVerifier(Of RemovePrivateMethodNeverUsedAnalyzer, RemovePrivateMethodNeverUsedCodeFixProvider)

        <Theory>
        <InlineData("Fact")>
        <InlineData("ContractInvariantMethod")>
        <InlineData("System.Diagnostics.Contracts.ContractInvariantMethod")>
        <InlineData("DataMember")>
        Public Async Function DoesNotGenerateDiagnosticsWhenMethodAttributeIsAnException(value As String) As Task
            Dim source = "
Class Foo
    <" + value + ">
    Private Sub PrivateFoo()
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function MainMethodEntryPointReturningVoidDoesNotCreateDiagnostic() As Task
            Const test = "
Module Foo
    Sub Main(args as String())
    End Sub
End Module"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function MainMethodEntryPointReturningIntegerDoesNotCreateDiagnostic() As Task
            Const test = "
Module Foo
    Function Main(args as String()) as Integer
        Return 0
    End Function
End Module"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function MainMethodEntryPointWithoutParameterDoesNotCreateDiagnostic() As Task
            Const test = "
Module Foo
    Function Main() as Integer
        Return 0
    End Function
End Module"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

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

        <Fact>
        Public Async Function WhenPrivateMethodHandlesEventExplicitlyDoesNotGenerateDiagnostics() As Task
            Const test = "
Imports System
Class Foo
    Private Event Load(ByVal sender As Object, ByVal e As EventArgs)

    Private Sub Form_Load(ByVal sender As Object, ByVal e As EventArgs) Handles Me.Load
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function
    End Class

End Namespace