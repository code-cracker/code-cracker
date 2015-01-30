Imports CodeCracker.Design
Imports CodeCracker.Test.TestHelper
Imports Xunit

Namespace Design
    Public Class EmptyCatchBlockTests
        Inherits CodeFixTest(Of EmptyCatchBlockAnalyzer, EmptyCatchBlockCodeFixProvider)

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
            Const testWithBlock = "
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
        Public Async Function WhenRemoveTryCatchStatement() As Task
            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Async Function Foo() As Task
            Dim a = ""A""
        End Function
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
        Public Async Function Foo() As Task
            Try
                Dim a = ""A""
            Catch ex As Exception
                Throw
            End Try
        End Function
    End Class
End Namespace"

            Await VerifyBasicFixAsync(test, fix, 1)
        End Function
    End Class
End Namespace