Imports CodeCracker.Performance
Imports CodeCracker.Test.TestHelper
Imports Microsoft.CodeAnalysis
Imports Xunit

Namespace Performance
    Public Class MakeLocalVariablesConstWhenItIsPossibleTests
        Inherits CodeFixTest(Of MakeLocalVariableConstWhenPossibleAnalyzer, MakeLocalVariableConstWhenPossibleCodeFixProvider)

        <Fact>
        Public Async Function IgnoresConstantDeclarations() As Task
            Dim test = "const a as Integer = 10".WrapInMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresDeclarationsWithNoInitializers() As Task
            Dim test = "dim a as Integer".WrapInMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresDeclarationsWithNonConstants() As Task
            Dim test = "Dim a as Integer = GetValue()".WrapInMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresDeclarationsWithReferenceTypes() As Task
            Dim test = "Dim a as New Foo()".WrapInMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresVariablesThatChangesValueOutsideDeclarations() As Task
            Dim test = "Dim a as Integer = 10 : a = 20".WrapInMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function CreateDiagnosticsWhenAssigningAPotentialConstant() As Task
            Dim test = "Dim a As Integer = 10".WrapInMethod()
            Dim expected = New DiagnosticResult With
        {
            .Id = MakeLocalVariableConstWhenPossibleAnalyzer.DiagnosticId,
            .Message = "This variable can be made const.",
            .Severity = DiagnosticSeverity.Info,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 6, 13)}
        }
            Await VerifyDiagnosticsAsync(test, expected)
        End Function

        <Fact>
        Public Async Function CreateDiagnosticsWhenAssigningAPotentialConstantUsingTypeInference() As Task
            Dim test = "Dim a = 10".WrapInMethod()
            Dim expected = New DiagnosticResult With
        {
            .Id = MakeLocalVariableConstWhenPossibleAnalyzer.DiagnosticId,
            .Message = "This variable can be made const.",
            .Severity = DiagnosticSeverity.Info,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 6, 13)}
        }
            Await VerifyDiagnosticsAsync(test, expected)
        End Function

        <Fact>
        Public Async Function CreateDiagnosticsWhenAssigningNothingToAReferenceType() As Task
            Dim test = "Dim a As Foo = Nothing".WrapInMethod()
            Dim expected = New DiagnosticResult With
        {
            .Id = MakeLocalVariableConstWhenPossibleAnalyzer.DiagnosticId,
            .Message = "This variable can be made const.",
            .Severity = DiagnosticSeverity.Info,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 6, 13)}
        }
            Await VerifyDiagnosticsAsync(test, expected)
        End Function

        <Fact>
        Public Async Function IgnoresNullableVariables() As Task
            Dim test = "Dim a As Integer? = 1".WrapInMethod()
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenDeclarationSpecifiesTypeName() As Task
            Dim test = "Dim a As Integer = 10".WrapInMethod()
            Dim expected = "Const a As Integer = 10".WrapInMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenDeclarationInfersType() As Task
            Dim test = "Dim a = 10".WrapInMethod()
            Dim expected = "Const a = 10".WrapInMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenDeclarationInfersString() As Task
            Dim test = "Dim a = """"".WrapInMethod()
            Dim expected = "Const a = """"".WrapInMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenSettingNullToAReferenceType() As Task
            Dim test = "Dim a As Foo = Nothing".WrapInMethod()
            Dim expected = "Const a As Foo = Nothing".WrapInMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenInferingType() As Task
            Dim test = "Dim a = 10".WrapInMethod()
            Dim expected = "Const a = 10".WrapInMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

    End Class
End Namespace