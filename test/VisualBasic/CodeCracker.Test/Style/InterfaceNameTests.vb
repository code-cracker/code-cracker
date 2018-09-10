Imports CodeCracker.VisualBasic.Style
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Style
    Public Class InterfaceNameTests
        Inherits CodeFixVerifier(Of InterfaceNameAnalyzer, InterfaceNameCodeFixProvider)
        <Fact>
        Public Async Function InterfaceNameStartsWithLetterI() As Task
            Const source = "Namespace ConsoleApplication1
    Public Interface IFoo
        Sub Test()
    End Interface
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function InterfaceNameNotStartsWithLetterI() As Task
            Const source = "Namespace ConsoleApplication1
    Public Interface Foo
        Sub Test()
    End Interface
End Namespace"

            Dim expected = New DiagnosticResult(DiagnosticId.InterfaceName.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Info) _
                .WithLocation(2, 5) _
                .WithMessage(InterfaceNameAnalyzer.MessageFormat)

            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function ChangeInterfaceNameWithoutI() As Task
            Const source = "Namespace ConsoleApplication1
    Public Interface Foo
        Sub Test()
    End Interface
End Namespace"
            Const fix = "Namespace ConsoleApplication1
    Public Interface IFoo
        Sub Test()
    End Interface
End Namespace"
            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function ChangeInterfaceNameWithoutIAndClassImplementation() As Task
            Const source = "Namespace ConsoleApplication1
    Public Interface Foo
        Sub Test()
    End Interface
    Public Class Test
        Implements Foo
        Public Sub Test() Implements Foo.Test
        End Sub
    End Class
End Namespace"
            Const fix = "Namespace ConsoleApplication1
    Public Interface IFoo
        Sub Test()
    End Interface
    Public Class Test
        Implements IFoo
        Public Sub Test() Implements IFoo.Test
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAsync(source, fix)
        End Function

        <Fact>
        Public Async Function ChangeAllInterfaceNamesWithoutI() As Task
            Const source1 = "Namespace ConsoleApplication1
    Public Interface Foo1
        Sub Test()
    End Interface
End Namespace"
            Const source2 = "Namespace ConsoleApplication2
    Public Interface Foo2
        Sub Test()
    End Interface
End Namespace"

            Const fix1 = "Namespace ConsoleApplication1
    Public Interface IFoo1
        Sub Test()
    End Interface
End Namespace"
            Const fix2 = "Namespace ConsoleApplication2
    Public Interface IFoo2
        Sub Test()
    End Interface
End Namespace"

            Await VerifyBasicFixAllAsync(New String() {source1, source2}, New String() {fix1, fix2})
        End Function

        <Fact>
        Public Async Function ChangeAllInterfaceNamesWithoutIAndClassImplementation() As Task
            Const source1 = "Namespace ConsoleApplication1
    Public Interface Foo1
        Sub Test()
    End Interface

    Public Class Test1
        Implements Foo1
        Public Sub Test() Implements Foo1.Test
        End Sub
    End Class
End Namespace"
            Const source2 = "Namespace ConsoleApplication2
    Public Interface Foo2
        Sub Test()
    End Interface

    Public Class Test2
        Implements Foo2
        Public Sub Test() Implements Foo2.Test
        End Sub
    End Class
End Namespace"

            Const fix1 = "Namespace ConsoleApplication1
    Public Interface IFoo1
        Sub Test()
    End Interface

    Public Class Test1
        Implements IFoo1
        Public Sub Test() Implements IFoo1.Test
        End Sub
    End Class
End Namespace"
            Const fix2 = "Namespace ConsoleApplication2
    Public Interface IFoo2
        Sub Test()
    End Interface

    Public Class Test2
        Implements IFoo2
        Public Sub Test() Implements IFoo2.Test
        End Sub
    End Class
End Namespace"

            Await VerifyBasicFixAllAsync(New String() {source1, source2}, New String() {fix1, fix2})
        End Function

    End Class
End Namespace