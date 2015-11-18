Imports CodeCracker.VisualBasic.Design
Imports Xunit

Namespace Design
    Public Class EmptyCatchBlockTests
        Inherits CodeFixVerifier(Of EmptyCatchBlockAnalyzer, EmptyCatchBlockCodeFixProvider)

        Private test As String = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Try
                Dim a = ""A""
            Catch
            End Try
        End Sub
    End Class
End Namespace"

        <Fact>
        Public Async Function EmptyCatchBlockAnalyzerCreateDiagnostic() As Task
            Const testWithBlock = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Try
                Dim a = ""A""
            Catch
                Throw
            End Try
        End Sub
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(testWithBlock)
        End Function

        <Fact>
        Public Async Function WhenRemoveTryCatchStatement() As Task
            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim a = ""A""
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(test, fix)
        End Function

        <Fact>
        Public Async Function WhenPutExceptionClassInCatchBlock() As Task
            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Try
                Dim a = ""A""
            Catch ex As Exception
                Throw
            End Try
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(test, fix, 1)
        End Function

        <Fact>
        Public Async Function WhenMultipleCatchRemoveOnlySelectedEmpty() As Task
            Const multipleTest As String = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Async Function Foo() As Task
            Try
                Dim a = ""A""
            Catch aex As ArgumentException
            Catch ex As Exception
                a = ""B""
            End Try
        End Function
    End Class
End Namespace"

            Const multipleFix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Async Function Foo() As Task
            Try
                Dim a = ""A""
            Catch ex As Exception
                a = ""B""
            End Try
        End Function
    End Class
End Namespace"

            Await VerifyBasicFixAsync(multipleTest, multipleFix, 0)

        End Function
    End Class
End Namespace