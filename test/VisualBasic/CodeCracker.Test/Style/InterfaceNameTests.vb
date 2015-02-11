Imports CodeCracker.VisualBasic.Style
Imports CodeCracker.Test.TestHelper
Imports Xunit

Namespace Style
    Public Class InterfaceNameTests
        Inherits CodeFixTest(Of InterfaceNameAnalyzer, InterfaceNameCodeFixProvider)
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

            Dim expected = New DiagnosticResult With {
                .Id = DiagnosticId.InterfaceName.ToDiagnosticId(),
                .Message = InterfaceNameAnalyzer.MessageFormat,
                .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Info,
                .Locations = {New DiagnosticResultLocation("Test0.vb", 2, 5)}
            }

            Await VerifyDiagnosticsAsync(source, expected)
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

    End Class
End Namespace