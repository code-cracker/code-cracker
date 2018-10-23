Imports CodeCracker.VisualBasic.Style
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Style
    Public Class TernaryOperatorWithAssignmentTests
        Inherits CodeFixVerifier(Of TernaryOperatorAnalyzer, TernaryOperatorWithAssignmentCodeFixProvider)

        <Fact>
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
        Public Async Function WhenUsingIfElseIfElseDoesNotCreate() As Task
            Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            Dim x = 0
            If 1 > 2 Then
                x = 1
            ElseIf 2 > 3 Then
                x = 2
            Else
                x = 3
            End If
        End Sub
    End Class
End Namespace"
            Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
        End Function

        <Fact>
        Public Async Function WhenUsingIfAndElseWithDirectReturnAnalyzerCreatesDiagnostic() As Task
            Dim expected = New DiagnosticResult(DiagnosticId.TernaryOperator_Assignment.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(8, 13) _
                .WithMessage("You can use a ternary operator.")
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
        ExcelRecord.imported = If(lCell Is Nothing, False, DirectCast(If(lCell.Value, lCell.Value.ToString = ""X""), Boolean))
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
        Public Async Function FixConsidersBaseTypeAssignent() As Task
            Const source = "
Public Class Base
End Class
Public Class B
    Inherits Base
End Class
Public Class MyType
    Public Sub Foo()
        Dim c As Base
        If True Then
            c = New Base()
        Else
            c = New B()
        End If
    End Sub
End Class"

            Const fix = "
Public Class Base
End Class
Public Class B
    Inherits Base
End Class
Public Class MyType
    Public Sub Foo()
        Dim c As Base
        c = If(True, New Base(), DirectCast(New B(), Base))
    End Sub
End Class"

            ' Allowing new diagnostics because without it the test fails because the compiler says Integer? is not defined.
            Await VerifyBasicFixAsync(source, fix, allowNewCompilerDiagnostics:=True)
        End Function
        <Fact>
        Public Async Function WhenUsingCommentsConcatenateAtEndOfTernary() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim a As Integer
        If True Then
            ' a
            a = 1 ' One Thing
        Else
            ' b
            a = 2 ' Another
        End If
    End Sub
End Class"

            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim a As Integer
        ' a
        ' b
        a = If(True, 1, 2) ' One Thing ' Another
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

        <Fact>
        Public Async Function WhenUsingConcatenationAssignmentExpandsToConcatenateAtEndOfTernary() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim x = ""test""
        If True Then
            x = ""1""
        Else
            x &= ""2""
        End If
    End Sub
End Class"
            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim x = ""test""
        x = If(True, ""1"", x & ""2"")
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function WhenUsingAddAssiginmentExpandsOperationProperly() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        If True Then
            x = 1
        Else
            x += 1
        End If
    End Sub
End Class"
            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        x = If(True, 1, x + 1)
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function WhenUsingSubtractAssiginmentExpandsOperationProperly() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        If True Then
            x = 1
        Else
            x -= 1
        End If
    End Sub
End Class"
            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        x = If(True, 1, x - 1)
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function WhenUsingConcatenationAssignmentOnElseAssignWithPlusExpandsToConcatenateAtEndOfTernary() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim x = ""test""
        If True Then
            x = ""1""
        Else
            x += ""2""
        End If
    End Sub
End Class"
            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim x = ""test""
        x = If(True, ""1"", x + ""2"")
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function WhenUsingConcatenationAssignmentOnIfAssignWithPlusExpandsToConcatenateAtEndOfTernary() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim x = ""test""
        If True Then
            x += ""1""
        Else
            x = ""2""
        End If
    End Sub
End Class"
            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim x = ""test""
        x = If(True, x + ""1"", ""2"")
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function WhenUsingConcatenationAssignmentOnIfAssignWithAmpersandExpandsToConcatenateAtEndOfTernary() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim x = ""test""
        If True Then
            x &= ""1""
        Else
            x = ""2""
        End If
    End Sub
End Class"
            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim x = ""test""
        x = If(True, x & ""1"", ""2"")
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function WhenUsingAddAssiginmentOnIfAssignExpandsOperationProperly() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        If True Then
            x += 1
        Else
            x = 1
        End If
    End Sub
End Class"
            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        x = If(True, x + 1, 1)
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function WhenUsingSubtractAssignmentOnIfAssignExpandsOperationProperly() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        If True Then
            x -= 1
        Else
            x = 1
        End If
    End Sub
End Class"
            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        x = If(True, x - 1, 1)
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function WhenUsingAssignmentOperatorReturnSameAssignment() As Task
            Const source = "
Class MyType
    Public Sub x2()
            Dim output As String = String.Empty
            Dim test As Boolean
            test = True
            If test Then
                output += ""True""
            Else
                output += ""False""
            End If
        End Sub
End Class"

            Const fix = "
Class MyType
    Public Sub x2()
            Dim output As String = String.Empty
            Dim test As Boolean
            test = True
        output += If(test, ""True"", ""False"")
        End Sub
End Class"

            Await VerifyBasicFixAsync(source, fix, formatBeforeCompare:=True)
        End Function

        <Fact>
        Public Async Function WhenUsingDifferentAssiginmentsExpandsOperationProperly() As Task
            Const source = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        If True Then
            x += 1
        Else
            x -= 1
        End If
    End Sub
End Class"

            Const fix = "
Public Class MyType
    Public Sub Foo()
        Dim x = 0
        x = If(True, x + 1, x - 1)
    End Sub
End Class"
            Await VerifyBasicFixAsync(source, fix)
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
        Public Async Function WhenUsingIfElseIfElseDoesNotCreate() As Task
            Const sourceWithMultipleStatements = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            If 1 > 2 Then
                Return 1
            ElseIf 2 > 3 Then
                Return 2
            Else
                Return 3
            End If
        End Sub
    End Class
End Namespace"
            Await VerifyBasicHasNoDiagnosticsAsync(sourceWithMultipleStatements)
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
            Dim expected = New DiagnosticResult(DiagnosticId.TernaryOperator_Return.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(6, 13) _
                .WithMessage("You can use a ternary operator.")
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

        <Fact>
        Public Async Function FixWhenThereIsNumericImplicitConversion() As Task
            Dim source = "
Function OnReturn() As Double
    Dim condition = True
    Dim aDouble As Double = 2
    Dim bInteger = 3
    If condition Then
        Return aDouble
    Else
        Return bInteger
    End If
End Function".WrapInVBClass()
            Dim fix = "
Function OnReturn() As Double
    Dim condition = True
    Dim aDouble As Double = 2
    Dim bInteger = 3
    Return If(condition, aDouble, bInteger)
End Function".WrapInVBClass()
            Await VerifyBasicFixAsync(source, fix, formatBeforeCompare:=True)
        End Function

        <Fact>
        Public Async Function FixWhenThereIsEnumImplicitConversionToNumeric() As Task
            Dim source = "
Enum Values
    Value
End Enum
Function OnReturn() As Double
    Dim condition = True
    Dim anEnum As Values = Values.Value
    Dim bInteger = 3
    If condition Then
        Return anEnum
    Else
        Return bInteger
    End If
End Function".WrapInVBClass()
            Dim fix = "
Enum Values
    Value
End Enum
Function OnReturn() As Double
    Dim condition = True
    Dim anEnum As Values = Values.Value
    Dim bInteger = 3
    Return If(condition, anEnum, bInteger)
End Function".WrapInVBClass()
            Await VerifyBasicFixAsync(source, fix, formatBeforeCompare:=True)
        End Function

        <Fact>
        Public Async Function FixCanWorkWithCommentsOnIf() As Task
            Const source = "
Function s() As Boolean
    If True Then
        ' a comment
        Return False 'b comment
    Else
        Return True
    End If
End Function"
            Const fix = "
Function s() As Boolean
    ' a comment
    Return If(True, False, True) 'b comment
End Function"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        Public Async Function FixCanWorkWithCommentsOnElse() As Task
            Const source = "
Function s() As Boolean
    If True Then
        Return False
    Else
        ' a comment
        Return True 'b comment
    End If
End Function"
            Const fix = "
Function s() As Boolean
    ' a comment
    Return If(True, False, True) 'b comment
End Function"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        Public Async Function FixCanWorkWithCommentsOnIfAndElse() As Task
            Const source = "
Function s() As Boolean
    If True Then
        ' a comment
        Return False 'b comment
    Else
        ' c comment
        Return True 'd comment
    End If
End Function"
            Const fix = "
Function s() As Boolean
    ' a comment
    ' c comment
    Return If(True, False, True) 'b comment 'd comment
End Function"
            Await VerifyBasicFixAsync(source, fix)
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

            Dim expected = New DiagnosticResult(DiagnosticId.TernaryOperator_Iif.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(5, 16) _
                .WithMessage("You can use a ternary operator.")

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
End Namespace
