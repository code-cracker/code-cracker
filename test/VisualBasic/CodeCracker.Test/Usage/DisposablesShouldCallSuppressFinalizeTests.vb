Imports CodeCracker.VisualBasic.Usage
Imports Xunit

Namespace Usage
    Public Class DisposablesShouldCallSuppressFinalizeTests
        Inherits CodeFixVerifier(Of DisposablesShouldCallSuppressFinalizeAnalyzer, DisposablesShouldCallSuppressFinalizeCodeFixProvider)

        <Theory>
        <InlineData("Structure")>
        <InlineData("Class")>
        Public Async Function WarningIfStructImplementsIDisposableWithNoSuppressFinalizeCall(type As String) As Task
            Dim test = String.Format("
Public {0} MyType
    Implements System.IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End {0}
", type)
            Dim expected = New DiagnosticResult With {
                .Id = DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(),
                .Message = "'MyType' should call GC.SuppressFinalize inside the Dispose method.",
                .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 5, 16)}
            }

            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Theory>
        <InlineData("Structure")>
        <InlineData("Class")>
        Public Async Function NoWarningIfDoesNotImplementIDisposable(type As String) As Task
            Dim test = String.Format("
Public {0} MyType
End {0}", type)
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Theory>
        <InlineData("Structure")>
        <InlineData("Class")>
        Public Async Function WhenImplementsIDisposableCallSuppressFinalize(type As String) As Task
            Dim source = String.Format("
Imports System
Public {0} MyType
    Implements System.IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        Dim x = 123
    End Sub
End {0}", type)

            Dim fix = String.Format("
Imports System
Public {0} MyType
    Implements System.IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        Dim x = 123
        GC.SuppressFinalize(Me)
    End Sub
End {0}", type)
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function WhenClassHasParameterizedDisposeMethod() As Task
            Const source = "
Imports System
Public Class MyType
    Implements System.IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(true)
    End Sub
    Protected Overridable Sub Dispose(disposing As Boolean)
    End Sub
End Class"

            Const fix = "
Imports System
Public Class MyType
    Implements System.IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        Dispose(true)
    End Sub
    Protected Overridable Sub Dispose(disposing As Boolean)
        GC.SuppressFinalize(Me)
    End Sub
End Class"

            Await VerifyBasicFixAsync(source, fix)

        End Function

    End Class
End Namespace