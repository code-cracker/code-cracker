Imports CodeCracker.VisualBasic.Usage
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Usage
    Public Class DisposableFieldNotDisposedTests
        Inherits CodeFixVerifier(Of DisposableFieldNotDisposedAnalyzer, DisposableFieldNotDisposedCodeFixProvider)

        <Fact>
        Public Async Function FieldNotDisposableDoesNotCreateDiagnostic() As Task
            Const source = "
Namespace ConsoleApplication1
    Class TypeName
        Private i As Integer
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function WhenAFieldThatImplementsIDisposableIsAssignedThroughAMethodCallCreatesDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Private field As D = D.Create()
    End Class
    Class D
        Implements IDisposable

        Public Shared Create = Function() New D()

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Info) _
                .WithLocation(5, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))
            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhenAFieldDeclarationIsNotAssignedDoesNotCreateDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Private field As D
    End Class
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function WhenAFieldDeclarationHas2VariableItCreates2Diagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Private field As D = D.Create()
        Private field2 As D = D.Create()
    End Class
    Class D
        Implements IDisposable

        Public Shared Create = Function() New D()

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Info) _
                .WithLocation(5, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))
            Dim expected2 = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Info) _
                .WithLocation(6, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field2"))

            Await VerifyBasicDiagnosticAsync(source, expected, expected2)
        End Function

        <Fact>
        Public Async Function WhenAFieldThatImplementsIDisposableIsDisposedOnATypeThatIsNotDisposableCreatesDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Private field As D = D.Create()
        Public Sub Dispose
            field.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable

        Public Shared Create = Function() New D()

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Info) _
                .WithLocation(5, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhenAFieldThatImplementsIDisposableIsNotDisposedCreatesDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As D = D.Create()
        Public Sub Dispose Implements IDisposable.Dispose
        End Sub
    End Class
    Class D
        Implements IDisposable

        Public Shared Create = Function() New D()

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Info) _
                .WithLocation(6, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhenAFieldThatImplementsIDisposableIsDisposedDoesNotCreateDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As D = D.Create()
        Public Sub Dispose Implements IDisposable.Dispose
            field.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable

        Public Shared Create = Function() New D()

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function WithStructCreatesDiagnostic() As Task
            Const source = "
Option Infer On
Option Strict On
Imports System
Namespace ConsoleApplication1
    Structure TypeName
        Private field As New D()
    End Class
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(7, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WithPartialClassCreatesDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Partial Class TypeName
        Implements IDisposable
    End Class
    Class TypeName
        Private field As New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(8, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhenAFieldThatImplementsIDisposableIsCallingIncorrectDisposeCreatesDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As New D()
        Public Sub Dispose() Implements IDisposable.Dispose
            field.Dispose(False)
        End Sub
    End Class
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
        Public Sub Dispose(disposing As Boolean)
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(6, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhenAFieldThatImplementsIDisposableIsBeingDisposedNotOnCorrectDisposeCreateDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As New D()
        Public Sub Dispose(value As Boolean)
            field.Dispose(False)
        End Sub

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(6, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhenAFieldThatImplementsIDisposableIsAssignedThroughAnObjectConstructionCreateDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Private field As New D()
    End Class
    Class D
        Implements IDisposable

        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Created.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(5, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function WhenAnIDisposablefieldIsAssignedThroughAMethodCallCreatesDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Private field As D = D.Create()
    End Class
    Class D
        Implements IDisposable
        Public Shared Create = Function() New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.DisposableFieldNotDisposed_Returned.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Info) _
                .WithLocation(5, 17) _
                .WithMessage(String.Format(DisposableFieldNotDisposedAnalyzer.MessageFormat, "field"))

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCallWithoutSimplifiedTypes() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Private field As D = D.Create()
    End Class
    Class D
        Implements IDisposable
        Public Shared Create = Function() New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As D = D.Create()

        Public Sub Dispose() Implements IDisposable.Dispose
            field.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Shared Create = Function() New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCall() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Private field As D = D.Create()
    End Class
    Class D
        Implements IDisposable
        Public Shared Function Create() As D
            Return New D()
        End Function
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As D = D.Create()

        Public Sub Dispose() Implements IDisposable.Dispose
            field.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Shared Function Create() As D
            Return New D()
        End Function
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function
        <Fact>
        Public Async Function FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCallAndEnclosingClassHasBaseListOfInheritance() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class OtherClass
    End Class
    Class TypeName
        Inherits OtherClass
        Private field As D = D.Create()
    End Class
    Class D
        Implements IDisposable
        Public Shared Create = Function() New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class OtherClass
    End Class
    Class TypeName
        Inherits OtherClass
        Implements IDisposable
        Private field As D = D.Create()

        Public Sub Dispose() Implements IDisposable.Dispose
            field.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Shared Create = Function() New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function
        <Fact>
        Public Async Function FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCallAndDisposeMethodAlreadyExists() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As D = D.Create()
        Private field2 As D = D.Create()
        Public Sub Dispose() Implements IDisposable.Dispose
            field2.Dispose() ' Comment1
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Shared Create = Function() New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As D = D.Create()
        Private field2 As D = D.Create()
        Public Sub Dispose() Implements IDisposable.Dispose
            field2.Dispose() ' Comment1
            field.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Shared Create = Function() New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixAFieldThatImplementsIDisposableAndIsAssignedThroughAMethodCallAndDisposeMethodAlreadyExistsAndEnclosingTypeImplementsIDipososable() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As D = D.Create()
        Private field2 As D = D.Create()
        Public Sub Dispose() Implements IDisposable.Dispose
            field2.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Shared Create = Function() New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As D = D.Create()
        Private field2 As D = D.Create()
        Public Sub Dispose() Implements IDisposable.Dispose
            field2.Dispose()
            field.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Shared Create = Function() New D()
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixAFieldThatImplementsIDisposableAndIsAssignedThroughObjectCreation() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Private field As New D()
    End Class
    Class D
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Const fix = "
Imports System
Namespace ConsoleApplication1
    Class TypeName
        Implements IDisposable
        Private field As New D()

        Public Sub Dispose() Implements IDisposable.Dispose
            field.Dispose()
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function FixWithDisposeMethodOnPartialClass() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    Partial Class TypeName
        Private field As New D()
    End Class
    Class TypeName
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Const fix = "
Imports System
Namespace ConsoleApplication1
    Partial Class TypeName
        Private field As New D()' Add field.Dispose() to the Dispose method on the partial file.
    End Class
    Class TypeName
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function DisposableFieldOnAbstractClassWithAbstractDisposableDoesNotCreateDiagnostic() As Task
            Const source = "
Imports System
Namespace ConsoleApplication1
    MustInherit Class TypeName
        Implements IDisposable
        Private field As New D()
        Public MustInherit Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
    Class D
        Implements IDisposable
        Public Sub Dispose() Implements IDisposable.Dispose
        End Sub
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function
    End Class

End Namespace

