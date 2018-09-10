Imports CodeCracker.VisualBasic.Performance
Imports Microsoft.CodeAnalysis.Testing
Imports Xunit

Namespace Performance
    Public Class SealedAttributeTests
        Inherits CodeFixVerifier(Of SealedAttributeAnalyzer, SealedAttributeCodeFixProvider)

        <Fact>
        Public Async Function ApplySealedWhenClassInheritsFromSystemAttributeClass() As Task
            Const test = "
Public Class MyAttribute
    Inherits System.Attribute
End Class"

            Dim expected = New DiagnosticResult(SealedAttributeAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(2, 14) _
                .WithMessage("Mark 'MyAttribute' as NotInheritable.")

            Await VerifyBasicDiagnosticAsync(test, expected)

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

            Dim expected = New DiagnosticResult(SealedAttributeAnalyzer.Id, Microsoft.CodeAnalysis.DiagnosticSeverity.Warning) _
                .WithLocation(6, 14) _
                .WithMessage("Mark 'OtherAttribute' as NotInheritable.")

            Await VerifyBasicDiagnosticAsync(test, expected)
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
        Public Async Function WhenSealedModifierIsAppliedOnClass() As Task
            Const test = "
Public Class MyAttribute
    Inherits System.Attribute
End Class"

            Const fix = "
Public NotInheritable Class MyAttribute
    Inherits System.Attribute
End Class"

            Await VerifyBasicFixAsync(test, fix, 0)
        End Function

        <Fact>
        Public Async Function WhenSealedModifierIsAppliedOnClassFixAll() As Task
            Const test1 = "
Public Class MyAttribute1
    Inherits System.Attribute
End Class"
            Const fix1 = "
Public NotInheritable Class MyAttribute1
    Inherits System.Attribute
End Class"

            Const test2 = "
Public Class MyAttribute2
    Inherits System.Attribute
End Class"
            Const fix2 = "
Public NotInheritable Class MyAttribute2
    Inherits System.Attribute
End Class"

            Await VerifyBasicFixAllAsync(New String() {test1, test2}, New String() {fix1, fix2})
        End Function
    End Class
End Namespace