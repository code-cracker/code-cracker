﻿Imports Microsoft.CodeAnalysis.Diagnostics
Imports CodeCracker.Refactoring
Imports Xunit

Namespace Refactoring
    Public Class AllowMembersOrderingAnalyzerTests
        Inherits TestHelper.CodeFixVerifier

        Protected Overrides Function GetDiagnosticAnalyzer() As DiagnosticAnalyzer
            Return New AllowMembersOrderingAnalyzer()
        End Function

        <Theory>
        <InlineData("Class")>
        <InlineData("Structure")>
        Public Async Function AllowMembersOrderingForEmptyTypeShouldNotTriggerDiagnostic(typeDeclaration As String) As Task
            Dim test = String.Format("{0} Foo
End {0}", typeDeclaration)
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Theory>
        <InlineData("Class")>
        <InlineData("Structure")>
        Public Async Function AllowMembersOrderingForOneMemberShouldNotTriggerDiagnostic(typeDeclaration As String) As Task
            Dim test = String.Format("{0} Foo
    Function bar()
        Return 0
    End Function
End {0}", typeDeclaration)
            Await VerifyBasicHasNoDiagnosticsAsync(test)
        End Function

        <Theory>
        <InlineData("Class")>
        <InlineData("Structure")>
        Public Async Function AllowMembersOrderingForMoreThanOneMembersHouldTriggerDiagnostic(typeDeclaration As String) As Task
            Dim test = String.Format("{0} Foo
    Function bar()
        Return 0
    End Function
    Sub car()
    End Sub    
End {0}", typeDeclaration)

            Dim expected = New TestHelper.DiagnosticResult With {
                .Id = AllowMembersOrderingAnalyzer.DiagnosticId,
                .Message = AllowMembersOrderingAnalyzer.MessageFormat,
                .Severity = Microsoft.CodeAnalysis.DiagnosticSeverity.Hidden,
                .Locations = {New TestHelper.DiagnosticResultLocation("Test0.vb", 2, 14 + typeDeclaration.Length)}
                }
            Await VerifyDiagnosticsAsync(test, expected)
        End Function
    End Class
End Namespace