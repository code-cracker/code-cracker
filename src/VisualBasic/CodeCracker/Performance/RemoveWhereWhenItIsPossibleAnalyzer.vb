Imports Microsoft.CodeAnalysis
Imports Microsoft.CodeAnalysis.Diagnostics
Imports Microsoft.CodeAnalysis.VisualBasic
Imports Microsoft.CodeAnalysis.VisualBasic.Syntax
Imports System.Collections.Immutable
Imports CodeCracker.Extensions

Namespace Performance
    <DiagnosticAnalyzer(LanguageNames.VisualBasic)>
    Public Class RemoveWhereWhenItIsPossibleAnalyzer
        Inherits DiagnosticAnalyzer

        Public Shared ReadOnly Id As String = DiagnosticId.RemoveWhereWhenItIsPossible.ToDiagnosticId()
        Public Const Title As String = "You should remove the 'Where' invocation when it is possible."
        Public Const MessageFormat As String = "You can remove 'Where' moving the predicate to '{0}'."
        Public Const Category As String = SupportedCategories.Performance
        Public Const Description As String = "When a LINQ operator supports a predicate parameter it should be used instead of using 'Where' followed by the operator"
        Protected Shared Rule As DiagnosticDescriptor = New DiagnosticDescriptor(
            Id,
            Title,
            MessageFormat,
            Category,
            DiagnosticSeverity.Warning,
            isEnabledByDefault:=True,
            description:=Description,
            helpLink:=HelpLink.ForDiagnostic(Id))

        Public Overrides ReadOnly Property SupportedDiagnostics() As ImmutableArray(Of DiagnosticDescriptor) = ImmutableArray.Create(Rule)

        Shared ReadOnly supportedMethods() As String = {"First", "FirstOrDefault", "Last", "LastOrDefault", "Any", "Single", "SingleOrDefault", "Count"}

        Public Overrides Sub Initialize(context As AnalysisContext)
            context.RegisterSyntaxNodeAction(AddressOf AnalyzeNode, SyntaxKind.InvocationExpression)
        End Sub

        Private Sub AnalyzeNode(context As SyntaxNodeAnalysisContext)
            Dim whereInvoke = DirectCast(context.Node, InvocationExpressionSyntax)
            If GetNameOfTheInvokeMethod(whereInvoke) <> "Where" Then Exit Sub

            Dim nextMethodInvoke = whereInvoke.Parent.FirstAncestorOrSelf(Of InvocationExpressionSyntax)()

            Dim candidate = GetNameOfTheInvokeMethod(nextMethodInvoke)
            If Not supportedMethods.Contains(candidate) Then Exit Sub

            If nextMethodInvoke.ArgumentList.Arguments.Any Then Return

            Dim diag = Diagnostic.Create(Rule, GetNameExpressionOfTheInvokedMethod(whereInvoke).GetLocation(), candidate)

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
End Namespace