Imports CodeCracker.VisualBasic.Refactoring
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Testing
Imports System.Threading.Tasks
Imports Xunit

Namespace Refactoring
    Public Class ChangeAnyToAllTests
        Inherits CodeFixVerifier(Of ChangeAnyToAllAnalyzer, ChangeAnyToAllCodeFixProvider)

        <Theory>
        <InlineData("
            Dim ints = {1, 2}.AsQueryable()
            Dim query = ints.Any(Function(i) i > 0)", 30, DiagnosticId.ChangeAnyToAll)>
        <InlineData("
            Dim ints = {1, 2}.AsQueryable()
            Dim query = Not ints.Any(function(i) i > 0)", 34, DiagnosticId.ChangeAnyToAll)>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(function(i) i > 0)", 30, DiagnosticId.ChangeAnyToAll)>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = Not ints.Any(function(i) i > 0)", 34, DiagnosticId.ChangeAnyToAll)>
        <InlineData("
            Dim ints = {1, 2}.AsQueryable()
            Dim query = Not ints.All(function(i) i > 0)", 34, DiagnosticId.ChangeAllToAny)>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = Not ints.All(function(i) i > 0)", 34, DiagnosticId.ChangeAllToAny)>
        Public Async Function AnyAndAllWithLinqCreatesDiagnostic(code As String, column As Integer, diagnosticId As DiagnosticId) As Task
            Dim source = code.WrapInVBMethod(imports:="
Imports System.Linq")
            Dim expected = New DiagnosticResult(diagnosticId.ToDiagnosticId(), DiagnosticSeverity.Hidden) _
                .WithLocation(9, column) _
                .WithMessage(If(diagnosticId = DiagnosticId.ChangeAnyToAll, ChangeAnyToAllAnalyzer.MessageAny, ChangeAnyToAllAnalyzer.MessageAll))
            Await VerifyBasicDiagnosticAsync(source, expected)
        End Function

        <Fact>
        Public Async Function ComplexLambdaDoesNotCreateDiagnostic() As Task
            Dim source = "
            Dim ints = {1, 2}
            Dim notAll = Not ints.All(Function(i) 
                    Const zero As Integer = 0
                    Return i > zero
                End Function)".WrapInVBMethod([imports]:="
Imports System.Linq")
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function InvokingMethodThatIsNotAnyDoesNotCreateDiagnostic() As Task
            Dim source = "Console.WriteLine(1)".WrapInVBMethod
            Await VerifyBasicHasNoDiagnosticsAsync(source)
        End Function

        <Fact>
        Public Async Function ConvertsConditionalExpression() As Task
            Dim original = "
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) If(True, True, False))".WrapInVBMethod([imports]:="
Imports System.Linq")
            Dim fix = "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) If(True, True, False) = False)".WrapInVBMethod([imports]:="
Imports System.Linq")
            Await VerifyBasicFixAsync(original, fix)
        End Function

        <Theory>
        <InlineData("
            Dim ints = {1, 2}
            ints.Any(Function(i) True)")>
        <InlineData("
            Dim ints = {1, 2}
            ints.All(Function(i) True)")>
        Public Async Function ExpressionStatementsDoNotCreateDiagnostic(code As String) As Task
            Dim original = code.WrapInVBMethod([imports]:="
Imports System.Linq")
            Await VerifyBasicHasNoDiagnosticsAsync(original)
        End Function

        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) i > 1)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i <= 1)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) i >= 1)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i < 1)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) i < 1)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i >= 1)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) i <> 1)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i = 1)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) Not True)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) True)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) i > Not(i = 1))", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i <= Not (i = 1))")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) True)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) False)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) False)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) True)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) True = (i = 1))", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) False = (i = 1))")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) (i = 1) = False)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i = 1)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) False = (i = 1))", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i = 1)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) (i = 1) <> True)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i = 1)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) True <> (i = 1))", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i = 1)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) (i = 1) <> False)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) (i = 1) = False)")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) False <> (i = 1))", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) False = (i = 1))")>
        <InlineData("
            Dim ints = {1, 2}
            Dim query = ints.Any(Function(i) i = 1)", "
            Dim ints = {1, 2}
            Dim query = Not ints.All(Function(i) i <> 1)")>
        <Theory>
        Public Async Function ConvertsSpecialCases(original As String, fix As String) As Task
            Await VerifyBasicFixAsync(original.WrapInVBMethod([imports]:="
Imports System.Linq"), fix.WrapInVBMethod([imports]:="
Imports System.Linq"))
        End Function
    End Class
End Namespace