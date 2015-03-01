Imports CodeCracker.VisualBasic.Performance
Imports Microsoft.CodeAnalysis
Imports Xunit

Namespace Performance
    Public Class MakeLocalVariablesConstWhenItIsPossibleTests
        Inherits CodeFixVerifier(Of MakeLocalVariableConstWhenPossibleAnalyzer, MakeLocalVariableConstWhenPossibleCodeFixProvider)

        <Fact>
        Public Async Function IgnoresConstantDeclarations() As Task
            Dim test = "const a as Integer = 10".WrapInVBMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresDeclarationsWithNoInitializers() As Task
            Dim test = "dim a as Integer".WrapInVBMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresDeclarationsWithNonConstants() As Task
            Dim test = "Dim a as Integer = GetValue()".WrapInVBMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresDeclarationsWithReferenceTypes() As Task
            Dim test = "Dim a as New Foo()".WrapInVBMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function IgnoresVariablesThatChangesValueOutsideDeclarations() As Task
            Dim test = "Dim a as Integer = 10 : a = 20".WrapInVBMethod
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function CreateDiagnosticsWhenAssigningAPotentialConstant() As Task
            Dim test = "Dim a As Integer = 10".WrapInVBMethod()
            Dim expected = New DiagnosticResult With
        {
            .Id = MakeLocalVariableConstWhenPossibleAnalyzer.Id,
            .Message = "This variable can be made const.",
            .Severity = DiagnosticSeverity.Info,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 6, 13)}
        }
            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function CreateDiagnosticsWhenAssigningAPotentialConstantUsingTypeInference() As Task
            Dim test = "Dim a = 10".WrapInVBMethod()
            Dim expected = New DiagnosticResult With
        {
            .Id = MakeLocalVariableConstWhenPossibleAnalyzer.Id,
            .Message = "This variable can be made const.",
            .Severity = DiagnosticSeverity.Info,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 6, 13)}
        }
            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function CreateDiagnosticsWhenAssigningNothingToAReferenceType() As Task
            Dim test = "Dim a As Foo = Nothing".WrapInVBMethod()
            Dim expected = New DiagnosticResult With
        {
            .Id = MakeLocalVariableConstWhenPossibleAnalyzer.Id,
            .Message = "This variable can be made const.",
            .Severity = DiagnosticSeverity.Info,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 6, 13)}
        }
            Await VerifyBasicDiagnosticAsync(test, expected)
        End Function

        <Fact>
        Public Async Function IgnoresNullableVariables() As Task
            Dim test = "Dim a As Integer? = 1".WrapInVBMethod()
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenDeclarationSpecifiesTypeName() As Task
            Dim test = "Dim a As Integer = 10".WrapInVBMethod()
            Dim expected = "Const a As Integer = 10".WrapInVBMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenDeclarationInfersType() As Task
            Dim test = "Dim a = 10".WrapInVBMethod()
            Dim expected = "Const a = 10".WrapInVBMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenDeclarationInfersString() As Task
            Dim test = "Dim a = """"".WrapInVBMethod()
            Dim expected = "Const a = """"".WrapInVBMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenSettingNullToAReferenceType() As Task
            Dim test = "Dim a As Foo = Nothing".WrapInVBMethod()
            Dim expected = "Const a As Foo = Nothing".WrapInVBMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

        <Fact>
        Public Async Function FixMakesAVariableConstWhenInferingType() As Task
            Dim test = "Dim a = 10".WrapInVBMethod()
            Dim expected = "Const a = 10".WrapInVBMethod()
            Await VerifyBasicFixAsync(test, expected)
        End Function

    End Class
End Namespace