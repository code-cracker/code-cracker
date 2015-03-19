Imports CodeCracker.VisualBasic.Style
Imports Xunit

Public Class TernaryOperatorWithAssignmentTests
    Inherits CodeFixVerifier(Of TernaryOperatorAnalyzer, TernaryOperatorWithAssignmentCodeFixProvider)

    Public Async Function WhenUsingIfWithoutElseAnalyzerDoesNotCreateDiagnostic() As Task
        Const sourceWithoutElse = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            If something Then
                x = ""b""
            End If
        End Sub
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithoutElse)

        Dim x = IIf(True, 1, 2)
    End Function

    <Fact>
    Public Async Function WhenUsingIfWithElseButWithBlockWith2StatementsOnIfAnalyzerDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            If something Then
                x = String.Empty
                x = ""c""
            Else
                x = ""d""
            End If
        End Sub
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    <Fact>
    Public Async Function WhenUsingIfWithElseButWithBlockWith2StatementsOnElseAnalyzerDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            If something Then
                x = ""B""
            Else
                Dim a = String.Empty
                x = ""C""
            End If
        End Sub
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    <Fact>
    Public Async Function WhenUsingIfWithElseButWithoutReturnOnElseDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            Dim y = ""c""
            If something Then
                x = ""b""
            Else
                Dim a = String.Empty
            End If
        End Sub
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    <Fact>
    Public Async Function WhenUsingIfWithElseButIfBlockWithoutReturnDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            dim y = ""b""
            If something Then
                x = ""b""
            Else
                y = x
            End If
        End Sub
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    <Fact>
    Public Async Function WhenUsingIElseIfDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            If something Then
                x = ""b""
            ElseIf Not Something then
                x = ""c""
                x = ""d""
            End If
        End Sub
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    <Fact>
    Public Async Function WhenUsingIfAndElseWithDirectReturnAnalyzerCreatesDiagnostic() As Task
        Dim expected As New DiagnosticResult With {
            .Id = DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId(),
            .Message = "You can use a ternary operator.",
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 8, 13)}
            }
        Await VerifyBasicDiagnosticAsync(sourceAssign, expected)
    End Function

    Private Const sourceAssign = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            If something Then
                x = ""b""
            Else
                x = ""c""
            End If
        End Sub
    End Class
End Namespace"


    <Fact>
    Public Async Function WhenUsingIfAndElseWithAssignmentChangeToTernaryFix() As Task
        Dim fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            x = If(something, ""b"", ""c"")
        End Sub
    End Class
End Namespace"
        Await VerifyBasicFixAsync(sourceAssign, fix)
    End Function
End Class

Public Class TernaryOperatorWithReturnTests
    Inherits CodeFixVerifier(Of TernaryOperatorAnalyzer, TernaryOperatorWithReturnCodeFixProvider)



    <Fact>
    Public Async Function WhenUsingIfWithoutElseAnalyzerDoesNotCreateDiagnostic() As Task
        Const sourceWithoutElse = "
Namespace ConsoleApplication1
    Class TypeName
        Public Function Foo() As Integer
            Dim something = true
            If something Then
                Return 1
            End If
        End Function
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithoutElse)
    End Function

    <Fact>
    Public Async Function WhenUsingIfWithElseButWithBlockWith2StatementsOnIfAnalyzerDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Function Foo() As Integer
            Dim something = true
            If something Then
                Dim a = String.Empty
                Return 1
            Else
                Return 2
            End If
        End Function
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    <Fact>
    Public Async Function WhenUsingIfWithElseButWithBlockWith2StatementsOnElseAnalyzerDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Function Foo() As Integer
            Dim something = true
            If something Then
                Return 1
            Else
                Dim a = String.Empty
                Return 2
            End If
        End Function
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    <Fact>
    Public Async Function WhenUsingIfWithElseButWithoutReturnOnElseDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Function Foo() As Integer
            Dim something = true
            If something Then
                Return 1
            Else
                Dim a = String.Empty
            End If
        End Function
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    <Fact>
    Public Async Function WhenUsingIfWithElseButIfBlockWithoutReturnDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Function Foo() As Integer
            Dim something = true
            If something Then
                Dim a = String.Empty
            Else
                Return 2
            End If
        End Function
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    <Fact>
    Public Async Function WhenUsingIElseIfDoesNotCreate() As Task
        Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Function Foo() As Integer
            Dim something = true
            If something Then
                Return 1
            ElseIf Not Something then
                Dim a = String.Empty
                Return 2
            End If
        End Function
    End Class
End Namespace"
        Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
    End Function

    Private Const sourceReturn = "
Namespace ConsoleApplication1
    Class TypeName
        Public Function Foo() As Integer
            Dim something = true
            If something Then
                Return 1
            Else
                Return 2
            End If
        End Function
    End Class
End Namespace"

    <Fact>
    Public Async Function WhenUsingIfAndElseWithDirectReturnAnalyzerCreatesDiagnostic() As Task
        Dim expected As New DiagnosticResult With {
            .Id = DiagnosticId.TernaryOperator_Return.ToDiagnosticId(),
            .Message = "You can use a ternary operator.",
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 6, 13)}
            }
        Await VerifyBasicDiagnosticAsync(sourceReturn, expected)
    End Function

    <Fact>
    Public Async Function WhenUsingIfAndElseWithDirectReturnCreatesFix() As Task
        Const fix = "
Namespace ConsoleApplication1
    Class TypeName
        Public Function Foo() As Integer
            Dim something = true
            Return If(something, 1, 2)
        End Function
    End Class
End Namespace"

        Await VerifyBasicFixAsync(sourceReturn, fix)
    End Function
End Class

Public Class TernaryOperatorFromIifTests
    Inherits CodeFixVerifier(Of TernaryOperatorAnalyzer, TernaryOperatorFromIifCodeFixProvider)

    <Fact>
    Public Async Function WhenUsingIifAndSimpleAssignmentCreatesFix() As Task
        Const source = "
Class TypeName
    Public Sub Foo()
        Dim x = 1
        x = Iif(x = 1, 2, 3)
    End Sub
End Class"

        Const fix = "
Class TypeName
    Public Sub Foo()
        Dim x = 1
        x = If(x = 1, 2, 3)
    End Sub
End Class"

        Await VerifyBasicFixAsync(source, fix)
    End Function

    <Fact>
    Public Async Function WhenUsingIifAndReturnCreatesFix() As Task
        Const source = "
Class TypeName
    Public Function Foo() As Integer
        Dim x = 1
        Return Iif(x = 1, 2, 3)
    End Function
End Class"

        Const fix = "
Class TypeName
    Public Function Foo() As Integer
        Dim x = 1
        Return If(x = 1, 2, 3)
    End Function
End Class"

        Dim expected As New DiagnosticResult With {
            .Id = DiagnosticId.TernaryOperator_Iif.ToDiagnosticId(),
            .Message = "You can use a ternary operator.",
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 5, 16)}
            }

        Await VerifyBasicDiagnosticAsync(source, expected)
        Await VerifyBasicFixAsync(source, fix)
    End Function

    <Fact>
    Public Async Function WhenNotUsingIifDoesNotCreateAnalyzer() As Task
        Const source = "
Class TypeName
    Public Sub Foo()
        Dim x = 1
    End Sub
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(source)
    End Function

    <Fact>
    Public Async Function WhenIifWithTooManyParametersDoesNotCreateAnalyzer() As Task
        Const source = "
Class TypeName
    Public Sub Foo()
        Dim x = Iif(true, 1, 2, 3)
    End Sub
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(source)
    End Function

    <Fact>
    Public Async Function WhenIifWithTooFewParametersDoesNotCreateAnalyzer() As Task
        Const source = "
Class TypeName
    Public Sub Foo()
        Dim x = Iif(true, 1)
    End Sub
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(source)
    End Function

End Class