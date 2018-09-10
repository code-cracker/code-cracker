Imports CodeCracker.VisualBasic.Performance
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Performance
    Public Class RemoveWhereWhenItIsPossibleTests
        Inherits CodeFixVerifier(Of RemoveWhereWhenItIsPossibleAnalyzer, RemoveWhereWhenItIsPossibleCodeFixProvider)

        <Theory>
        <InlineData("First")>
        <InlineData("FirstOrDefault")>
        <InlineData("Last")>
        <InlineData("LastOrDefault")>
        <InlineData("Any")>
        <InlineData("Single")>
        <InlineData("SingleOrDefault")>
        <InlineData("Count")>
        Public Async Function CreateDiagnosticWhenUsingWhereWith(method As String) As Task
            Dim test = "
Imports System.Linq
Namespace Sample
    Public Class Foo
        Public Async Function DoSomething() As Task
            Dim a(10) As Integer
            Dim f = a.Where(Function(item) item > 10)." + method + "()
        End Function
    End Class
End Namespace"

            Dim expected = New DiagnosticResult(RemoveWhereWhenItIsPossibleAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(7, 23) _
                .WithMessage("You can remove 'Where' moving the predicate to '" + method + "'.")

            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("FirstOrDefault")>
        <InlineData("Last")>
        <InlineData("LastOrDefault")>
        <InlineData("Any")>
        <InlineData("Single")>
        <InlineData("SingleOrDefault")>
        <InlineData("Count")>
        Public Async Function DoNotCreateDiagnosticWheUsingWhereAndAnotherMethodWithPredicates(method As String) As Task
            Dim test = "
Imports System.Linq
Namespace Sample
    Public Class Foo
        Public Async Function DoSomething() As Task
            Dim a(10) As Integer
            Dim f = a.Where(Function(item) item > 10)." + method + "(function(item) item < 50)
        End Function
    End Class
End Namespace"

            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("FirstOrDefault")>
        <InlineData("Last")>
        <InlineData("LastOrDefault")>
        <InlineData("Any")>
        <InlineData("Single")>
        <InlineData("SingleOrDefault")>
        <InlineData("Count")>
        Public Async Function FixRemovesWhereMovingPredicateTo(method As String) As Task
            Dim test = "
Imports System.Linq
Namespace Sample
    Public Class Foo
        Public Async Function DoSomething() As Task
            Dim a(10) As Integer
            Dim f = a.Where(Function(item) item > 10)." + method + "()
        End Function
    End Class
End Namespace"

            Dim expected = "
Imports System.Linq
Namespace Sample
    Public Class Foo
        Public Async Function DoSomething() As Task
            Dim a(10) As Integer
            Dim f = a." & method & "(Function(item) item > 10)
        End Function
    End Class
End Namespace"

            Await VerifyBasicFixAsync(test, expected)
        End Function

        <Theory>
        <InlineData("First")>
        <InlineData("FirstOrDefault")>
        <InlineData("Last")>
        <InlineData("LastOrDefault")>
        <InlineData("Any")>
        <InlineData("Single")>
        <InlineData("SingleOrDefault")>
        <InlineData("Count")>
        Public Async Function FixRemovesWherePreservingPreviousExpressionsMovingPredicateTo(method As String) As Task
            Dim test = "
Imports System.Linq
Namespace Sample
    Public Class Foo
        Public Async Function DoSomething() As Task
            Dim a(10) As Integer
            Dim f = a.OrderBy(Function(item) item).Where(Function(item) item > 10)." + method + "()
        End Function
    End Class
End Namespace"

            Dim expected = "
Imports System.Linq
Namespace Sample
    Public Class Foo
        Public Async Function DoSomething() As Task
            Dim a(10) As Integer
            Dim f = a.OrderBy(Function(item) item)." & method & "(Function(item) item > 10)
        End Function
    End Class
End Namespace"

            Await VerifyBasicFixAsync(test, expected)
        End Function
    End Class
End Namespace