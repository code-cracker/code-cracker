Imports CodeCracker.VisualBasic.Refactoring
Imports Xunit

Namespace Refactoring
    Public Class ParameterRefactoryTests
        Inherits CodeFixTest(Of ParameterRefactoryAnalyzer, ParameterRefactoryCodeFixProvider)

        <Fact>
        Public Async Function WhenMethodDoesNotThreeParametersNotSuggestNewClass() As Task
            Const test = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo(name As String, age As String, day As String)
        End Sub
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function WhenMethodHasElementBodyAndHasMoreThanThreeParametersShouldNotSuggestNewClass() As Task
            Const test = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo(name As String, age As String, day As Integer, year As Integer)
            If True Then
                day = 10
            End If
        End Sub
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function WhenMethodHasByRefParameterShouldNotSuggestNewClass() As Task
            Const test = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo(a As String, b As String, year As Integer, ByRef d As String)
        End Sub
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function WhenExtensionMethodShouldNotSuggestNewClass() As Task
            Const test = "
Imports System
Namespace ConsoleApplication1
    Module TypeName
        <System.Runtime.CompilerServices.Extension>
        Public Sub Foo(name As String, age As String, day As String, d As String)
        End Sub
    End Module
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function WhenMethodHasParamArayShouldNotSuggestNewClass() As Task
            Const test = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo(name As String, age As String, ParamArray day() As String)
        End Sub
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function ShouldUpdateParameterToClass() As Task
            Const test = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo(a As String, b As String, year As Integer, d As String)
        End Sub
    End Class
End Namespace"

            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo(newClassFoo As NewClassFoo)
        End Sub
    End Class

    Public Class NewClassFoo
        Public Property A  As String
        Public Property B  As String
        Public Property Year  As Integer
        Public Property D  As String
    End Class
End Namespace"

            Await VerifyBasicFixAsync(test, fix, 0)
        End Function

        <Fact>
        Public Async Function WhenHasNotNamespaceShouldGenerateClassParameter() As Task
            Const test = "
Imports System
Class TypeName
    Public Sub Foo(a As String, b As String, year As Integer, d As String)
    End Sub
End Class"

            Const fix = "
Imports System
Class TypeName
    Public Sub Foo(newClassFoo As NewClassFoo)
    End Sub
End Class

Public Class NewClassFoo
    Public Property A  As String
    Public Property B  As String
    Public Property Year  As Integer
    Public Property D  As String
End Class
"

            Await VerifyBasicFixAsync(test, fix, 0)
        End Function

        <Fact>
        Public Async Function ShouldGenerateNewClassFoo2() As Task
            Const test = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo(newClassFoo As NewClassFoo)
        End Sub
        Public Sub Foo2(a As String, b As String, year As Integer, d As String)
        End Sub
    End Class
    Public Class NewClassFoo
        Public Property A As String
        Public Property B As String
        Public Property Year As Integer
        Public Property D As String
    End Class
End Namespace"

            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo(newClassFoo As NewClassFoo)
        End Sub
        Public Sub Foo2(newClassFoo2 As NewClassFoo2)
        End Sub
    End Class
    Public Class NewClassFoo
        Public Property A As String
        Public Property B As String
        Public Property Year As Integer
        Public Property D As String
    End Class

    Public Class NewClassFoo2
        Public Property A  As String
        Public Property B  As String
        Public Property Year  As Integer
        Public Property D  As String
    End Class
End Namespace"

            Await VerifyBasicFixAsync(test, fix, 0)
        End Function
    End Class
End Namespace