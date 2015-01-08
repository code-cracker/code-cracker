Imports CodeCracker.Test.TestHelper
Imports Xunit

Public Class EmptyCatchBlockTests
    Inherits CodeFixTest(Of EmptyCatchBlockAnalyzer, EmptyCatchBlockCodeFixProvider)
    Public Async Function Foo() As Task
        Dim b = 1
        Dim c = 2
        Try
            ' Do something
            Dim a = "A"
        Catch ex As Exception
            Dim x = 1
        End Try
    End Function

    Private test = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Async Function Foo() As Task
            Try
                Dim a = ""A""
            Catch
            End Try
        End Function
    End Class
End Namespace"

    <Fact>
    Public Async Function EmptyCatchBlockAnalyzerCreateDiagnostic() As Task
        Dim testWithBlock = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Async Function Foo() As Task
            Try
                Dim a = ""A""
            Catch
                Throw
            End Try
        End Function
    End Class
End Namespace"

        Await VerifyBasicHasNoDiagnosticsAsync(testWithBlock)
    End Function

    <Fact>
    Public Sub WhenRemoveTryCatchStatement()
        Dim fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Async Function Foo() As Task
            Dim a = ""A""
        End Function
    End Class
End Namespace"

        VerifyBasicFix(test, fix)
    End Sub

    <Fact>
    Public Sub WhenRemoveTryCatchStatementAndPutComment()
        Dim fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Async Function Foo() As Task
            Dim a = ""A""
            ' TODO: Consider reading MSDN Documentation about how to use Try...Catch => http://msdn.microsoft.com/en-us/library/0yd65esw.aspx
        End Function
    End Class
End Namespace"

        VerifyBasicFix(test, fix, 1)
    End Sub
    <Fact>
    Public Sub WhenPutExceptionClassInCatchBlock()
        Dim fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Async Function Foo() As Task
            Try
                Dim a = ""A""
            Catch ex As Exception
                Throw
            End Try
        End Function
    End Class
End Namespace"

        VerifyBasicFix(test, fix, 2)
    End Sub
End Class
