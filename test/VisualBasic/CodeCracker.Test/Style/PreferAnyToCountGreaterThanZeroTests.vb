Imports CodeCracker.VisualBasic.Style
Imports Microsoft.CodeAnalysis
Imports Xunit

Namespace Style
    Public Class PreferAnyToCountGreaterThanZeroTests
        Inherits CodeFixVerifier(Of PreferAnyToCountGreaterThanZeroAnalyzer, PreferAnyToCountGreaterThanZeroCodeFixProvider)



        Private Const Test As String = "Imports System.Linq
    Module Module1

        Sub Main()
            Dim ints = {1, 2}
            Dim query = True AndAlso ints.Count() > 0 AndAlso True
        End Sub

    End Module"

        <Fact>
        Public Async Function CreatesDiagnosticsWhenUsingCountGreaterThanZero() As Task
            Dim expected = New DiagnosticResult With
                {
                    .Id = DiagnosticId.PreferAnyToCountGreaterThanZero.ToDiagnosticId(),
                    .Message = String.Format(PreferAnyToCountGreaterThanZeroAnalyzer.MessageFormat.ToString(), ""),
                    .Severity = DiagnosticSeverity.Info,
                    .Locations = {New DiagnosticResultLocation("Test0.vb", 6, 38)}
                }

            Await VerifyBasicDiagnosticAsync(Test, expected)
        End Function

        <Fact>
        Public Async Function IgnoresCountWithoutZero() As Task
            Const TestCountProp As String = "Imports System.Linq
    Module Module1

        Sub Main()
            Dim list = New List(Of Integer)(New Integer() {1, 2})
            Dim query = list.Count() > 1
        End Sub

    End Module"

            Await VerifyBasicHasNoDiagnosticsAsync(TestCountProp)
        End Function

        <Fact>
        Public Async Function ConvertsCountPropertyToAny() As Task
            Const TestCountProp As String = "Imports System.Collections.Generic
Module Module1

    Sub Main()
        Dim list = New List(Of Integer)(New Integer() {1, 2})
        Dim query = list.Count > 0
    End Sub

End Module"

            Const FixTest As String = "Imports System.Collections.Generic
Imports System.Linq

Module Module1

    Sub Main()
        Dim list = New List(Of Integer)(New Integer() {1, 2})
        Dim query = list.Any()
    End Sub

End Module"

            Await VerifyBasicFixAsync(TestCountProp, FixTest)
        End Function

        <Fact>
        Public Async Function ConvertsCountMethodToAny() As Task
            Const FixTest As String = "Imports System.Linq
    Module Module1

        Sub Main()
            Dim ints = {1, 2}
            Dim query = True AndAlso ints.Any() AndAlso True
        End Sub

    End Module"

            Await VerifyBasicFixAsync(Test, FixTest)
        End Function

        <Fact>
        Public Async Function ConvertsToAnyWithPredicate() As Task
            Const TestPredicate As String = "Imports System.Linq
    Module Module1
        Class Bar
            Public A As Boolean
        End Class

        Sub Main()
            Dim bools = {New Bar(), New Bar()}
            Dim query = True AndAlso bools.Count(Function(x) x.A) > 0 AndAlso True
        End Sub

    End Module"

            Const FixTest As String = "Imports System.Linq
    Module Module1
        Class Bar
            Public A As Boolean
        End Class

        Sub Main()
            Dim bools = {New Bar(), New Bar()}
            Dim query = True AndAlso bools.Any(Function(x) x.A) AndAlso True
        End Sub

    End Module"

            Await VerifyBasicFixAsync(TestPredicate, FixTest)
        End Function
    End Class
End Namespace