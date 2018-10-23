Imports CodeCracker.VisualBasic.Usage
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Usage
    Public Class MustInheritClassShouldNotHavePublicConstructorTests
        Inherits CodeFixVerifier(Of MustInheritClassShouldNotHavePublicConstructorsAnalyzer, MustInheritClassShouldNotHavePublicConstructorsCodeFixProvider)

        <Fact>
        Public Async Function CreateDiagnosticWhenAMustInheritClassHavePublicConstructor() As Task
            Const test = "
MustInherit Class Foo
    Public Sub New
        ' Do Something
    End Sub
End Class"

            Dim expected = New DiagnosticResult(DiagnosticId.AbstractClassShouldNotHavePublicCtors.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(3, 5) _
                .WithMessage("Constructor should not be public.")
            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function IgnoresPublicConstructorInNonAbstractClasses() As Task
            Const test = "
Class Foo
    Public Sub New()
        ' Do Something
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresProtectedConstructorInMustInheritClasses() As Task
            Const test = "
MustInherit Class Foo
    Protected Sub New()
        ' Do Something
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresPrivateConstructorInNonMustInheritClasses() As Task
            Const test = "
MustInherit Class Foo
    Private Sub New()
        ' Do Something
    End Sub
End Class"
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function FixReplacesPublicWithProtectedModifierInMustInheritClasses() As Task
            Const test = "
MustInherit Class Foo
    Public Sub New
        ' Do Something
    End Sub
End Class"

            Const fix = "
MustInherit Class Foo
    Protected Sub New
        ' Do Something
    End Sub
End Class"

            Await VerifyBasicFixAsync(test, fix)
        End Function
    End Class
End Namespace
