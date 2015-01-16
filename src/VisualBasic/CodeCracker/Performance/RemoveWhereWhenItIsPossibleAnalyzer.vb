Imports System.Collections.Immutable
Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax

<DiagnosticAnalyzer(LanguageNames.VisualBasic)>
Public Class RemoveWhereWhenItIsPossibleAnalyzer
    Inherits CodeCrackerAnalyzerBase
    Public Sub New()
        MyBase.New(ID:=PerformanceDiagnostics.RemoveWhereWhenItIsPossibleId,
        Title:="You should remove the 'Where' invocation when it is possible.",
        MsgFormat:="You can remove 'Where' moving the predicate to '{0}'.",
        Category:=SupportedCategories.Performance,
        Description:="When a LINQ operator supports a predicate parameter it should be used instead of using 'Where' followed by the operator")
    End Sub

    Shared ReadOnly supportedMethods() As String = {"First", "FirstOrDefault", "Last", "LastOrDefault", "Any", "Single", "SingleOrDefault", "Count"}

    Public Overrides Sub OnInitialize(context As AnalysisContext)
        context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.InvocationExpression)
    End Sub

    Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
        Dim whereInvoke = DirectCast(context.Node, InvocationExpressionSyntax)
        If GetNameOfTheInvokeMethod(whereInvoke) <> "Where" Then Exit Sub

        Dim nextMethodInvoke = whereInvoke.Parent.FirstAncestorOrSelf(Of InvocationExpressionSyntax)()

        Dim candidate = GetNameOfTheInvokeMethod(nextMethodInvoke)
        If Not supportedMethods.Contains(candidate) Then Exit Sub

        If nextMethodInvoke.ArgumentList.Arguments.Any Then Return

        Dim diag = Diagnostic.Create(GetDescriptor(), GetNameExpressionOfTheInvokedMethod(whereInvoke).GetLocation(), candidate)

        context.ReportDiagnostic(diag)
    End Sub

    Friend Shared Function GetNameOfTheInvokeMethod(invoke As InvocationExpressionSyntax) As String
        If (invoke Is Nothing) Then Return Nothing
        Dim memberAccess = invoke.ChildNodes.
            OfType(Of MemberAccessExpressionSyntax).
            FirstOrDefault()

        Return GetNameExpressionOfTheInvokedMethod(invoke)?.ToString()
    End Function

    Friend Shared Function GetNameExpressionOfTheInvokedMethod(invoke As InvocationExpressionSyntax) As SimpleNameSyntax
        If invoke Is Nothing Then Return Nothing

        Dim memberAccess = invoke.ChildNodes.
            OfType(Of MemberAccessExpressionSyntax)().
            FirstOrDefault()

        Return memberAccess?.Name

    End Function

End Class
