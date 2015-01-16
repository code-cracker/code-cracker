Imports CodeCracker.Test.TestHelper
Imports Xunit

Public Class SealedAttributeTests
    Inherits CodeFixTest(Of SealedAttributeAnalyzer, SealedAttributeCodeFixProvider)

    <Fact>
    Public Async Function ApplySealedWhenClassInheritsFromSystemAttributeClass() As Task
        Const test = "
Public Class MyAttribute
    Inherits System.Attribute
End Class"

        Dim expected = New DiagnosticResult With
            {
            .Id = PerformanceDiagnostics.SealedAttributeId,
            .Message = "Mark 'MyAttribute' as MustInherit.",
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 2, 14)}
        }

        Await VerifyBasicDiagnosticsAsync(test, expected)

    End Function

    <Fact>
    Public Async Function ApplySealedWhenClassInheritsIndirectlyFromSystemAttributeClass() As Task
        Const test = "
Public MustInherit Class MyAttribute
    Inherits System.Attribute
End Class

Public Class OtherAttribute
    Inherits MyAttribute
End Class"

        Dim expected = New DiagnosticResult With
            {
            .Id = PerformanceDiagnostics.SealedAttributeId,
            .Message = "Mark 'OtherAttribute' as MustInherit.",
            .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Warning,
            .Locations = {New DiagnosticResultLocation("Test0.vb", 6, 14)}
        }

        Await VerifyBasicDiagnosticsAsync(test, expected)
    End Function

    <Fact>
    Public Async Function NotApplySealedWhenClassThatInheritsFromSystemAttributeClassIsAbstract() As Task
        Const test = "
Public MustInherit Class MyAttribute
    Inherits System.Attribute
End Class"

        Await VerifyBasicHasNoDiagnosticsAsync(test)
    End Function

    <Fact>
    Public Async Function NotApplySealedWhenClassThatInheritsFromSystemAttributeClassIsNotInheritable() As Task
        Const test = "
Public NotInheritable Class MyAttribute
    Inherits System.Attribute
End Class"
        Await VerifyBasicHasNoDiagnosticsAsync(test)
    End Function

    <Fact>
    Public Async Function NotApplyNotInheritableWhenIsStruct() As Task
        Const test = "
Public Structure MyStructure
End Structure"
        Await VerifyBasicHasNoDiagnosticsAsync(test)
    End Function

    <Fact>
    Public Async Function NotApplyNotInheritableWhenIsInterface() As Task
        Const test = "
Public Interface ITest
End Interface"
        Await VerifyBasicHasNoDiagnosticsAsync(test)
    End Function

    <Fact>
    Public Sub WhenSealedModifierIsAppliedOnClass()
        Const test = "
Public Class MyAttribute
    Inherits System.Attribute
End Class"

        Const fix = "
Public NotInheritable Class MyAttribute
    Inherits System.Attribute
End Class"

        VerifyBasicFix(test, fix, 0)
    End Sub
End Class