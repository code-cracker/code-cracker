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
    Class MyType
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
        Const fix = "
Imports System
Namespace ConsoleApplication1
    Class MyType
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            x = If(something, ""b"", ""c"")
        End Sub
    End Class
End Namespace"
        Await VerifyBasicFixAsync(sourceAssign, fix)
    End Function

    <Fact>
    Public Async Function WhenUsingIfAndElseWithAssignmentChangeToTernaryFixAll() As Task
        Const fix = "
Imports System
Namespace ConsoleApplication1
    Class MyType
        Public Sub Foo()
            Dim something = true
            Dim x = ""a""
            x = If(something, ""b"", ""c"")
        End Sub
    End Class
End Namespace"
        Await VerifyBasicFixAllAsync(New String() {sourceAssign, sourceAssign.Replace("MyType", "MyType1")}, New String() {fix, fix.Replace("MyType", "MyType1")})
    End Function

    <Fact>
    Public Async Function WhenTernaryWithObjectDoesApplyFix() As Task
        Const source = "
Class MyCustomer
    Public Property Value As String
End Class
Public Class ExcelLineRecordClass
    Public Property LineNumber As Integer
    Public Property imported As Boolean
End Class
Class Tester
    Private Sub Test()
        Dim ExcelRecord As New ExcelLineRecordClass
        Dim lCell As New MyCustomer
        If lCell Is Nothing Then
            ExcelRecord.imported = False
        Else
            ExcelRecord.imported = If(lCell.Value, lCell.Value.ToString = ""X"")
        End If
    End Sub
End Class"

        Const fix = "
Class MyCustomer
    Public Property Value As String
End Class
Public Class ExcelLineRecordClass
    Public Property LineNumber As Integer
    Public Property imported As Boolean
End Class
Class Tester
    Private Sub Test()
        Dim ExcelRecord As New ExcelLineRecordClass
        Dim lCell As New MyCustomer
        ExcelRecord.imported = If(lCell Is Nothing, False, If(lCell.Value, lCell.Value.ToString = ""X""))
    End Sub
End Class"

        Await VerifyBasicFixAsync(source, fix)

    End Function

    <Fact>
    Public Async Function WhenUsingIfAndElseWithNullableValueTypeAssignmentChangeToTernaryFix() As Task
        Const source = "
Public Class MyType
    Public Sub Foo()
        Dim a As Integer?
        If True Then
            a = 1
        Else
            a = Nothing
        End If
    End Sub
End Class"

        Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim a As Integer?
        a = If(True, 1, DirectCast(Nothing, Integer?))
    End Sub
End Class"

        ' Allowing new diagnostics because without it the test fails because the compiler says Integer? is not defined.
        Await VerifyBasicFixAsync(source, fix, allowNewCompilerDiagnostics:=True)
    End Function

    <Fact>
    Public Async Function WhenUsingIfAndElseWithNullableValueTypeAssignmentChangeToTernaryFixAll() As Task
        Const source = "
Public Class MyType
    Public Sub Foo()
        Dim a As Integer?
        If True Then
            a = 1
        Else
            a = Nothing
        End If
    End Sub
End Class"

        Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim a As Integer?
         a = If(True, 1, DirectCast(Nothing, Integer?))
    End Sub
End Class"

        Await VerifyBasicFixAllAsync(New String() {source, source.Replace("MyType", "MyType1")}, New String() {fix, fix.Replace("MyType", "MyType1")})
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

    <Fact>
    Public Async Function WhenUsingIfAndElseWithNullableValueTypeDirectReturnChangeToTernaryFix() As Task
        Const source = "
Public Class MyType
    Public Function Foo() As Integer?
        If True Then
            Return 1
        Else
            Return Nothing
        End If
    End Function
End Class"

        Const fix = "
Public Class MyType
    Public Function Foo() As Integer?
        Return If(True, 1, DirectCast(Nothing, Integer?))
    End Function
End Class"

        ' Allowing new diagnostics because without it the test fails because the compiler says Integer? is not defined.
        Await VerifyBasicFixAsync(source, fix, allowNewCompilerDiagnostics:=True)
    End Function

    <Fact>
    Public Async Function WhenUsingIfAndElseWithNullableValueTypeDirectReturnChangeToTernaryFixAll() As Task
        Const source = "
Public Class MyType
    Public Function Foo() As Integer?
        If True Then
            Return 1
        Else
            Return Nothing
        End If
    End Function
End Class"

        Const fix = "
Public Class MyType
    Public Function Foo() As Integer?
        Return If(True, 1, DirectCast(Nothing, Integer?))
    End Function
End Class"

        Await VerifyBasicFixAllAsync(New String() {source, source.Replace("MyType", "MyType1")}, New String() {fix, fix.Replace("MyType", "MyType1")})
    End Function

    Private Const sourceReturn = "
Namespace ConsoleApplication1
    Class MyType
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
    Class MyType
        Public Function Foo() As Integer
            Dim something = true
            Return If(something, 1, 2)
        End Function
    End Class
End Namespace"

        Await VerifyBasicFixAsync(sourceReturn, fix)
    End Function

    <Fact>
    Public Async Function WhenUsingIfAndElseWithDirectReturnCreatesFixAll() As Task
        Const fix = "
Namespace ConsoleApplication1
    Class MyType
        Public Function Foo() As Integer
            Dim something = true
            Return If(something, 1, 2)
        End Function
    End Class
End Namespace"

        Await VerifyBasicFixAllAsync(New String() {sourceReturn, sourceReturn.Replace("MyType", "MyType1")}, New String() {fix, fix.Replace("MyType", "MyType1")})
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
    Public Async Function WhenUsingIifAndSimpleAssignmentCreatesFixAll() As Task
        Const source = "
Class MyType
    Public Sub Foo()
        Dim x = 1
        x = Iif(x = 1, 2, 3)
    End Sub
End Class"

        Const fix = "
Class MyType
    Public Sub Foo()
        Dim x = 1
        x = If(x = 1, 2, 3)
    End Sub
End Class"

        Await VerifyBasicFixAllAsync(New String() {source, source.Replace("MyType", "MyType1")}, New String() {fix, fix.Replace("MyType", "MyType1")})
    End Function

    <Fact>
    Public Async Function WhenUsingIifAndReturnCreatesFix() As Task
        Const source = "
Class MyType
    Public Function Foo() As Integer
        Dim x = 1
        Return Iif(x = 1, 2, 3)
    End Function
End Class"

        Const fix = "
Class MyType
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
    Public Async Function WhenUsingIifAndReturnCreatesFixAll() As Task
        Const source = "
Class MyType
    Public Function Foo() As Integer
        Dim x = 1
        Return Iif(x = 1, 2, 3)
    End Function
End Class"

        Const fix = "
Class MyType
    Public Function Foo() As Integer
        Dim x = 1
        Return If(x = 1, 2, 3)
    End Function
End Class"

        Await VerifyBasicFixAllAsync(New String() {source, source.Replace("MyType", "MyType1")}, New String() {fix, fix.Replace("MyType", "MyType1")})
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

