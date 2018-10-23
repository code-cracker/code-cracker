Imports CodeCracker.VisualBasic.Reliability
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Reliability
    Public Class UseConfigureAwaitFalseTests
        Inherits CodeFixVerifier(Of UseConfigureAwaitFalseAnalyzer, UseConfigureAwaitFalseCodeFixProvider)

        <Theory>
        <InlineData("Dim t As System.Threading.Tasks.Task: Await t", 51)>
        <InlineData("Dim t As System.Threading.Tasks.Task: Await t.ContinueWith(Function() 42)", 51)>
        <InlineData("Await System.Threading.Tasks.Task.Delay(1000)", 13)>
        <InlineData("Await System.Threading.Tasks.Task.FromResult(0)", 13)>
        <InlineData("Await System.Threading.Tasks.Task.Run(Sub() )", 13)>
        <InlineData("Dim f As Func(Of System.Threading.Tasks.Task) : Await f()", 61)>
        Public Async Function WhenAwaitingTaskAnalyzerCreatesDiagnostic(sample As String, column As Integer) As Task
            Dim test = sample.WrapInVBMethod(isAsync:=True)
            Dim expected = New DiagnosticResult(DiagnosticId.UseConfigureAwaitFalse.ToDiagnosticId(), Microsoft.CodeAnalysis.DiagnosticSeverity.Hidden) _
                .WithLocation(6, column) _
                .WithMessage("Consider using ConfigureAwait(False) on the awaited task.")
            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Theory>
        <InlineData("Dim t As System.Threading.Tasks.Task: Await t.ConfigureAwait(false)")>
        <InlineData("Dim t As System.Threading.Tasks.Task: Await.ContinueWith(Function() 42).ConfigureAwait(false)")>
        <InlineData("Await System.Threading.Tasks.Task.Delay(1000).ConfigureAwait(False)")>
        <InlineData("Await System.Threading.Tasks.Task.FromResult(0).ConfigureAwait(False)")>
        <InlineData("Await System.Threading.Tasks.Task.Run(Sub()).ConfigureAwait(False)")>
        <InlineData("Dim f As Func(Of System.Treading.Tasks.Task) : Await f().ConfigureAwait(False)")>
        <InlineData("Await System.Threading.Tasks.Task.Yield()")>
        <InlineData("Await UnknownAsync()")>
        Public Async Function WhenAwaitingANonTaskNoDiagnosticIsCreated(sample As String) As Task
            Dim test = sample.WrapInVBMethod(isAsync:=True)
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Theory>
        <InlineData(
            "Dim t As System.Threading.Tasks.Task: Await t",
            "Dim t As System.Threading.Tasks.Task: Await t.ConfigureAwait(False)")>
        <InlineData(
            "Dim t As System.Threading.Tasks.Task: Await t.ContinueWith(Function() 42)",
            "Dim t As System.Threading.Tasks.Task: Await t.ContinueWith(Function() 42).ConfigureAwait(False)")>
        <InlineData(
            "Await System.Threading.Tasks.Task.Delay(1000)",
            "Await System.Threading.Tasks.Task.Delay(1000).ConfigureAwait(False)")>
        <InlineData(
            "Await System.Threading.Tasks.Task.FromResult(0)",
            "Await System.Threading.Tasks.Task.FromResult(0).ConfigureAwait(False)")>
        <InlineData(
            "Await System.Threading.Tasks.Task.Run(Sub() )",
            "Await System.Threading.Tasks.Task.Run(Sub() ).ConfigureAwait(False)")>
        <InlineData(
            "Dim f As Func(Of System.Threading.Tasks.Task): Await F()",
            "Dim f As Func(Of System.Threading.Tasks.Task): Await F().ConfigureAwait(False)")>
        Public Async Function FixAddsConfigureAwaitFalse(original As String, result As String) As Task
            Dim test = original.WrapInVBMethod(isAsync:=True)
            Dim fix = result.WrapInVBMethod(isAsync:=True)

            Await VerifyBasicFixAsync(test, fix)
        End Function

        <Theory>
        <InlineData(
            "Dim t As System.Threading.Tasks.Task: Await t",
            "Dim t As System.Threading.Tasks.Task: Await t.ConfigureAwait(False)")>
        <InlineData(
            "Dim t As System.Threading.Tasks.Task: Await t.ContinueWith(Function() 42)",
            "Dim t As System.Threading.Tasks.Task: Await t.ContinueWith(Function() 42).ConfigureAwait(False)")>
        <InlineData(
            "Await System.Threading.Tasks.Task.Delay(1000)",
            "Await System.Threading.Tasks.Task.Delay(1000).ConfigureAwait(False)")>
        <InlineData(
            "Await System.Threading.Tasks.Task.FromResult(0)",
            "Await System.Threading.Tasks.Task.FromResult(0).ConfigureAwait(False)")>
        <InlineData(
            "Await System.Threading.Tasks.Task.Run(Sub() )",
            "Await System.Threading.Tasks.Task.Run(Sub() ).ConfigureAwait(False)")>
        <InlineData(
            "Dim f As Func(Of System.Threading.Tasks.Task): Await F()",
            "Dim f As Func(Of System.Threading.Tasks.Task): Await F().ConfigureAwait(False)")>
        Public Async Function FixAllAddsConfigureAwaitFalse(original As String, result As String) As Task
            Dim test1 = original.WrapInVBMethod(isAsync:=True, typeName:="MyType1")
            Dim fix1 = result.WrapInVBMethod(isAsync:=True, typeName:="MyType1")

            Dim test2 = original.WrapInVBMethod(isAsync:=True, typeName:="MyType2")
            Dim fix2 = result.WrapInVBMethod(isAsync:=True, typeName:="MyType2")

            Await VerifyBasicFixAllAsync(New String() {test1, test2}, New String() {fix1, fix2})
        End Function
    End Class
End Namespace