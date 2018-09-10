Imports CodeCracker.VisualBasic.Performance
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Performance
    Public Class StringBuilderInLoopTests
        Inherits CodeFixVerifier(Of StringBuilderInLoopAnalyzer, StringBuilderInLoopCodeFixProvider)

        <Fact>
        Public Async Function WhileWithoutAddAssignmentExpressionDoesNotCreateDiagnostic() As Task
            Const source = "
Namespace ConsoleApplication1
    Class TypeName
        Public Sub Foo()
            While (DateTime.Now.Second Mod 2 = 0)
                Method()
            End While
        End Sub
        Public Sub Method()

        End Sub
    End Class
End Namespace"
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function WhileWithoutStringConcatDoesNotCreateDiagnostic() As Task
            Dim source = "
            Dim a = 0
            While A < 10
                a += 1
            End While".WrapInVBMethod()

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function WhileWithoutStringConcatWithMethoParameterDoesNotCreateDiagnostic() As Task
            Const source = "
    Public Class TypeName
        Public Sub Looper(ByRef a As Integer)
            While a < 10
                a += 1
            End While
        End Sub
    End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function WhileWithStringConcatOnLocalVariableCreateDiagnostic() As Task
            Dim source = "
            Dim a = """"
            While DateTime.Now.Second mod 2 = 10
                a += """"
            End While".WrapInVBMethod()

            Dim expected = GetExpected()

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhileWithStringConcatOnFieldVariableCreatesDiagnostic() As Task
            Const source = "
Namespace ConsoleApplication1
    Class TypeName
        Private a As String = """"
        Public Sub Foo()
            While (DateTime.Now.Second Mod 2 = 0)
                a += """"
            End While
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(StringBuilderInLoopAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(7, 17) _
                .WithMessage(String.Format(StringBuilderInLoopAnalyzer.MessageFormat, "a"))
            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        Private Shared Function GetExpected() As DiagnosticResult
            Return New DiagnosticResult(StringBuilderInLoopAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(9, 17) _
                .WithMessage(String.Format(StringBuilderInLoopAnalyzer.MessageFormat, "a"))
        End Function

        <Fact>
        Public Async Function WhileWithStringConcatOnLocalVariableCreatesDiagnostic() As Task
            Dim source = "
            Dim a = """"
            While DateTime.Now.Second mod 2 = 0
                a += """"
            End While
".WrapInVBMethod()

            Await VerifyBasicDiagnosticAsync(source, GetExpected())
        End Function

        <Fact>
        Public Async Function WhileWithStringConcatOnPropertyVariableCreatesDiagnostic() As Task
            Const source = "
Namespace ConsoleApplication1
    Class TypeName
        Private Property a As String = """"
        Public Sub Foo()
            While (DateTime.Now.Second Mod 2 = 0)
                a += """"
            End While
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(StringBuilderInLoopAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(7, 17) _
                .WithMessage(String.Format(StringBuilderInLoopAnalyzer.MessageFormat, "a"))

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function
        <Fact>
        Public Async Function WhileWithStringConcatWithSeveralConcatsOnDifferentVarsCreatesSeveralDiagnostics() As Task
            Dim source = "
            Dim a = """"
            Dim myString2 = """"
            While DateTime.Now.Second mod 2 = 0
                a += """"
                myString2 += """"
            End While
            Console.WriteLine(myString2)
".WrapInVBMethod()

            Dim expected1 = New DiagnosticResult(StringBuilderInLoopAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(10, 17) _
                .WithMessage(String.Format(StringBuilderInLoopAnalyzer.MessageFormat, "a"))
            Dim expected2 = New DiagnosticResult(StringBuilderInLoopAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(11, 17) _
                .WithMessage(String.Format(StringBuilderInLoopAnalyzer.MessageFormat, "myString2"))

            Await VerifyBasicDiagnosticAsync(source, expected1, expected2)
        End Function

        <Fact>
        Public Async Function WhileWithStringConcatWithSimpleAssignmentCreatesDiagnostic() As Task
            Dim source = "
            Dim a = """"
            While DateTime.Now.Second mod 2 = 0
                a = a + """"
            End While
".WrapInVBMethod()

            Await VerifyBasicDiagnosticAsync(source, GetExpected())
        End Function

        <Fact>
        Public Async Function WhileWithStringConcatWithSimpleAssignmentOnDifferentDimDoesNotCreateDiagnostic() As Task
            Dim source = "Dim a = """"
            Dim otherString = """"
            While DateTime.Now.Second Mod 2 = 0
                a = otherString + """"
            End While
".WrapInVBMethod
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function FixesAddAssignmentInWhile() As Task
            Dim source = "Dim a = """"
            While DateTime.Now.Second Mod 2 = 0
                a += ""a""
            End While
".WrapInVBMethod

            Dim fix = "Dim a = """"
            Dim builder As New Text.StringBuilder()
            builder.Append(a)
            While DateTime.Now.Second Mod 2 = 0
                builder.Append(""a"")
            End While
            a = builder.ToString()
".WrapInVBMethod()
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixesAddAssignmentInWhileWithSystemTextInContext() As Task
            Const source = "
Imports System
Imports System.Text
Namespace ConsoleApplication1

    Class TypeName
        Public Sub Test()
            Dim a = """"
            While (DateTime.Now.Second Mod 2 = 0)
                a += ""a""
            End While
        End Sub
    End Class
End Namespace"


            Const fix = "
Imports System
Imports System.Text
Namespace ConsoleApplication1

    Class TypeName
        Public Sub Test()
            Dim a = """"
            Dim builder As New StringBuilder()
            builder.Append(a)
            While (DateTime.Now.Second Mod 2 = 0)
                builder.Append(""a"")
            End While
            a = builder.ToString()
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixesSimpleAssignmentInWhile() As Task
            Dim source = "Dim a = """"
            ' comment 3
            While (DateTime.Now.Second Mod 2 = 0)
                ' comment 1
                a += ""a"" 'comment 2
            End While 'comment 4
".WrapInVBMethod

            Dim fix = "Dim a = """"
            Dim builder As New Text.StringBuilder()
            builder.Append(a)
            ' comment 3
            While (DateTime.Now.Second Mod 2 = 0)
                ' comment 1
                builder.Append(""a"") 'comment 2
            End While 'comment 4
            a = builder.ToString()
".WrapInVBMethod

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixesAddAssignmentWhenThereAre2WhilesOnBlock() As Task
            Dim source = "Dim a = """"
            While (DateTime.Now.Second Mod 2 = 0)
                Dim a = 1
            End While
            While (DateTime.Now.Second Mod 2 = 0)
                a += ""a""
            End While
".WrapInVBMethod()

            Dim fix = "Dim a = """"
            While (DateTime.Now.Second Mod 2 = 0)
                Dim a = 1
            End While
            Dim builder As New Text.StringBuilder()
            builder.Append(a)
            While (DateTime.Now.Second Mod 2 = 0)
                builder.Append(""a"")
            End While
            a = builder.ToString()
".WrapInVBMethod()

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixesAddAssignmentWithoutClashingTheBuilderName() As Task
            Dim source = "Dim builder = 1
            Dim a = """"
            While (DateTime.Now.Second Mod 2 = 0)
                a += ""a""
            End While
".WrapInVBMethod()

            Dim fix = "Dim builder = 1
            Dim a = """"
            Dim builder1 As New Text.StringBuilder()
            builder1.Append(a)
            While (DateTime.Now.Second Mod 2 = 0)
                builder1.Append(""a"")
            End While
            a = builder1.ToString()
".WrapInVBMethod()

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixesAddAssignmentWithoutClashingTheBuilderNameOnAField() As Task
            Const source = "
Namespace ConsoleApplication1

    Class TypeName
        Private builder As Integer
        Public Sub Foo()
            Dim builder = 1
            Dim a = """"
            While (DateTime.Now.Second Mod 2 = 0)
                a += ""a""
            End While
        End Sub
    End Class
End Namespace"


            Const fix = "
Namespace ConsoleApplication1

    Class TypeName
        Private builder As Integer
        Public Sub Foo()
            Dim builder = 1
            Dim a = """"
            Dim builder1 As New System.Text.StringBuilder()
            builder1.Append(a)
            While (DateTime.Now.Second Mod 2 = 0)
                builder1.Append(""a"")
            End While
            a = builder1.ToString()
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function ForWithStringConcatOnLocalVariableCreatesDiagnostic() As Task
            Dim source = "Dim a = """"
            For i As Integer = 1 To 10
                a += ""a""
            Next".WrapInVBMethod

            Dim fix = "Dim a = """"
            Dim builder As New Text.StringBuilder()
            builder.Append(a)
            For i As Integer = 1 To 10
                builder.Append(""a"")
            Next
            a = builder.ToString()".WrapInVBMethod

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixesAddAssignmentInFor() As Task
            Dim source = "Dim a = """"
            For i As Integer = 1 To 10
                a += ""b""
                Exit For
            Next".WrapInVBMethod

            Dim builder As New System.Text.StringBuilder()
            builder.Append("a")

            Dim fix = "Dim a = """"
            Dim builder As New Text.StringBuilder()
            builder.Append(a)
            For i As Integer = 1 To 10
                builder.Append(""b"")
                Exit For
            Next
            a = builder.ToString()".WrapInVBMethod

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function ForeachWithStringConcatOnLocalVariableCreatesDiagnostic() As Task
            Dim source = "
            Dim a = """"
            For Each i In {1, 2, 3}
                a += """"
            Next".WrapInVBMethod

            Dim expected = New DiagnosticResult(StringBuilderInLoopAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(9, 17) _
                .WithMessage(String.Format(StringBuilderInLoopAnalyzer.MessageFormat, "a"))
            Await VerifyBasicDiagnosticAsync(source, expected)

        End Function

        <Fact>
        Public Async Function FixesAddAssignmentInForEach() As Task
            Dim source = "Dim a = """"
            For Each i In {1, 2, 3}
                a += ""a""
            Next".WrapInVBMethod

            Dim fix = "Dim a = """"
            Dim builder As New Text.StringBuilder()
            builder.Append(a)
            For Each i In {1, 2, 3}
                builder.Append(""a"")
            Next
            a = builder.ToString()".WrapInVBMethod

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function DoWithStringConcatOnLocalVariableCreatesDiagnostic() As Task
            Dim source = "
            Dim a = """"
            Do
                a += """"
            Loop Until DateTime.Now.Second Mod 2 = 0
".WrapInVBMethod

            Dim expected = New DiagnosticResult(StringBuilderInLoopAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(9, 17) _
                .WithMessage(String.Format(StringBuilderInLoopAnalyzer.MessageFormat, "a"))
            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function FixesAddAssignmentInForDo() As Task
            Dim source = "Dim a = """"
            Do
                a += ""a""
            Loop Until DateTime.Now.Second Mod 2 = 0
".WrapInVBMethod

            Dim b As New System.Text.StringBuilder()

            Dim fix = "Dim a = """"
            Dim builder As New Text.StringBuilder()
            builder.Append(a)
            Do
                builder.Append(""a"")
            Loop Until DateTime.Now.Second Mod 2 = 0
            a = builder.ToString()
".WrapInVBMethod

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function ForLoopInMethodWithoutStringShouldNotCreateDiagnostic() As Task
            Const source = "
    Public Class Test
        Private Sub AdjustSample(ByRef readIndex As Integer, writeBuffer() As Single, ByRef writeIndex As Integer)
            For i = 0 To 2
                writeBuffer(writeIndex) = 0
                readIndex += 1
                writeIndex += 1
            Next
        End Sub
    End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function ForLoopStringDeclaredAndConcatenatedWithinShouldNotCreateDiagnostic() As Task
            Dim source = "
            For i = 0 To 10
                Dim someString As String = i.ToString()
                someString &= "" ""
                Console.WriteLine(someString)
            Next".WrapInVBMethod()

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function
    End Class
End Namespace