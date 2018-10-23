Imports CodeCracker.VisualBasic.Usage
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Usage
    Public Class ArgumentExceptionTests
        Inherits CodeFixVerifier(Of ArgumentExceptionAnalyzer, ArgumentExceptionCodeFixProvider)

        Shared Function Wrap(code As String) As String
            Return "
Using System

Namespace ConsoleApplication1
    Class TypeName
        " & code & "
    End Class
End Namespace"
        End Function

        <Fact>
        Public Async Function WhenThrowingArgumentExceptionWithInvalidArgumentAnalyzerCreatesDiagnostic() As Task
            Dim test = Wrap("
Public Async Function Foo(a As Integer, b As Integer) As Task
    Throw New ArgumentException(""message"", ""c"")
End Function
")

            Dim expected = New DiagnosticResult(DiagnosticId.ArgumentException.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(8, 44) _
                .WithMessage("Type argument 'c' is not in the argument list.")

            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function WhenThrowingArgumentExceptionInConstrucorWithInvalidARgumentAnalyzerCreatesDiagnostic() As Task
            Dim test = Wrap("
Public Sub New(a As Integer, b As Integer)
    Throw New ArgumentException(""message"", ""c"")
End Sub
")

            Dim expected = New DiagnosticResult(DiagnosticId.ArgumentException.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(8, 44) _
                .WithMessage("Type argument 'c' is not in the argument list.")

            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingFirstFixUsesFirstParameter() As Task
            Dim test = Wrap("
Public Sub Foo(a As Integer, b As Integer)
    Throw New ArgumentException(""message"", ""c"")
End Sub
")

            Dim fix = Wrap("
Public Sub Foo(a As Integer, b As Integer)
    Throw New ArgumentException(""message"", ""a"")
End Sub
")
            Await VerifyBasicFixAsync(test, fix, 0)
        End Function

        <Fact>
        Public Async Function WhenThrowingArgumentExceptionWithInvalidArgumentAndApplyingSecondFixUsesSecondParameter() As Task
            Dim test = Wrap("
Public Sub Foo(a As Integer, b As Integer)
    Throw New ArgumentException(""message"", ""c"")
End Sub
")

            Dim fix = Wrap("
Public Sub Foo(a As Integer, b As Integer)
    Throw New ArgumentException(""message"", ""b"")
End Sub
")
            Await VerifyBasicFixAsync(test, fix, 1)
        End Function

        <Fact>
        Public Async Function WhenThrowingArgumentExceptionWithInvalidArgumentInConstructorAndApplyingFirstFixUsesFirstParameter() As Task
            Dim test = Wrap("
Public Sub New(a As Integer, b As Integer)
    Throw New ArgumentException(""message"", ""c"")
End Sub
")

            Dim fix = Wrap("
Public Sub New(a As Integer, b As Integer)
    Throw New ArgumentException(""message"", ""a"")
End Sub
")
            Await VerifyBasicFixAsync(test, fix, 0)
        End Function

        <Fact>
        Public Async Function WhenThrowingArgumentExceptionWithInvalidArgumentInConstructorAndApplyingFirstFixUsesSecondParameter() As Task
            Dim test = Wrap("
Public Sub New(a As Integer, b As Integer)
    Throw New ArgumentException(""message"", ""c"")
End Sub
")

            Dim fix = Wrap("
Public Sub New(a As Integer, b As Integer)
    Throw New ArgumentException(""message"", ""b"")
End Sub
")
            Await VerifyBasicFixAsync(test, fix, 1)
        End Function

        <Fact>
        Public Async Function IgnoresAgrumentExceptionObjectsInFields() As Task
            Const test = "Dim ex As New ArgumentException(""message"", ""paramName"")"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresArgumentExceptionObjectsInGetAccessors() As Task
            Dim test = Wrap("
        Public Property NewProperty() As String
            Get
                Throw New ArgumentException(""message"", ""paramName"")
            End Get
            Set(ByVal value As String)
            End Set
        End Property
")
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function WhenThrowingArgumentExceptionInSetPropertyArgumentNameShouldBeValue() As Task
            Dim test = Wrap("
        Public Property NewProperty() As String
            Get
            End Get
            Set(ByVal value As String)
                Throw New ArgumentException(""message"", ""paramName"")
            End Set
        End Property
")
            Dim expected = New DiagnosticResult(DiagnosticId.ArgumentException.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(11, 56) _
                .WithMessage("Type argument 'paramName' is not in the argument list.")
            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function WhenThrowingArgmentExceptionWithInvalidArgumentInSetAccessorAndApplyingFixUsesParameter() As Task
            Dim test = Wrap("
        Public Property NewProperty() As String
            Get
                Return Nothing
            End Get
            Set(ByVal value As String)
                Throw New ArgumentException(""message"", ""paramName"")
            End Set
        End Property
")
            Dim fix = Wrap("
        Public Property NewProperty() As String
            Get
                Return Nothing
            End Get
            Set(ByVal value As String)
                Throw New ArgumentException(""message"", ""value"")
            End Set
        End Property
")

            Await VerifyBasicFixAsync(test, fix)
        End Function

        <Fact>
        Public Async Function WhenThrowingArgumentExceptionInMultilineLambdaAndApplyingFix() As Task
            Dim test = Wrap("
        Dim a = Function(p) 
                   Throw New ArgumentException(""message"", ""paramName"")
                End Function
        Sub Foo(a As String)
            Dim action = Sub(p1)
                Throw New ArgumentException(""message"", ""paramName"")
            End Sub        
        End Sub
")
            Dim fix = Wrap("
        Dim a = Function(p) 
                   Throw New ArgumentException(""message"", ""p"")
                End Function
        Sub Foo(a As String)
            Dim action = Sub(p1)
                Throw New ArgumentException(""message"", ""p1"")
            End Sub        
        End Sub
")

            Await VerifyBasicFixAsync(test, fix)
        End Function

        <Fact>
        Public Async Function IgnoresArgumentExceptionObjectsInInitializerOfAutoProperties() As Task
            Const test = "
ReadOnly Property Test = New ArgumentException(""message"", ""paramName"")
"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

    End Class
End Namespace