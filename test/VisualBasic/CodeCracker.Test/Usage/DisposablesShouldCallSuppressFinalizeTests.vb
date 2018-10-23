Imports CodeCracker.VisualBasic.Usage
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Usage
    Public Class DisposablesShouldCallSuppressFinalizeTests
        Inherits CodeFixVerifier(Of DisposablesShouldCallSuppressFinalizeAnalyzer, DisposablesShouldCallSuppressFinalizeCodeFixProvider)

        <Fact>
        Public Async Function WarningIfClassImplementsIDisposableWithNoSuppressFinalizeCall() As Task
            Const test = "
Public Class MyType
    Implements System.IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
"
            Dim expected = New DiagnosticResult(DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(5, 16) _
                .WithMessage("'MyType' should call GC.SuppressFinalize inside the Dispose method.")

            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function NoWarningIfClassImplmentsIDisposableButDoesNotContainsAPublicConstructor() As Task
            Const test = "
Public Class MyType
    Implements System.IDisposable

    Private Sub New()
    End Sub

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Protected Overrides Sub Finalize()
            MyBase.Finalize()
    End Sub
End Class
"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function FunctionNoWarningIfClassIsAPrivateNestedType() As Task
            Const test = "
Public Class MyType
    Private Class MyNestedType
        Implements System.IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub

        Protected Overrides Sub Finalize()
                MyBase.Finalize()
        End Sub
    End Class
End Class
"

            Await VerifyBasicHasNoDiagnosticsAsync(test)

        End Function

        <Fact>
        Public Async Function DoesNotWarnIfStructImplementsIDisposableWithNoSuppressFinalizeCall() As Task
            Const test = "
Public Struture MyType
    Implements System.IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Structure
"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function


        <Fact>
        Public Async Function DoesNotWarnIfSealedClassImplementsIDisposableWithNoSuppressFinalizeCall() As Task
            Const test = "
Public NotInheritable Class MyType
    Implements System.IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub
End Class
"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function


        <Fact>
        Public Async Function WarnIfSealedClassImplementsIDisposableWithNoSuppressFinalizeCallAndContainsUserDefinedFinalizer() As Task
            Const test = "
Public NotInheritable Class MyType
    Implements System.IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
    End Sub

    Protected Overrides Sub Finalize()
            MyBase.Finalize()
    End Sub
End Class
"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposablesShouldCallSuppressFinalize.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(5, 16) _
                .WithMessage("'MyType' should call GC.SuppressFinalize inside the Dispose method.")

            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function NoWarningIfClassImplementsDisposableCallsSuppressFinalizeAndCallsDisposeWithMe() As Task
            Const source = "
Public Class MyType
    Implements System.IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        Me.Dispose(True)
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function NoWarningIfClassImplementsDisposableCallsSuppressFinalize() As Task
            Const source = "
Public Class MyType
    Implements System.IDisposable

    Public Sub Dispose() Implements IDisposable.Dispose
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
    End Sub
End Class"

            Await VerifyBasicHasNoDiagnosticsAsync(source)
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

        <Fact>
        Public Async Function WhenImplementsIDisposableCallSuppressFinalize() As Task
            Const source = "
Imports System
Public Class MyType
    Implements System.IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        Dim x = 123
    End Sub
End Class"

            Const fix = "
Imports System
Public Class MyType
    Implements System.IDisposable
    Public Sub Dispose() Implements IDisposable.Dispose
        Dim x = 123
        GC.SuppressFinalize(Me)
    End Sub
End Class"
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
        GC.SuppressFinalize(Me)
    End Sub

    Protected Overridable Sub Dispose(disposing As Boolean)
    End Sub
End Class"

            Await VerifyBasicFixAsync(source, fix)

        End Function

    End Class

End Namespace